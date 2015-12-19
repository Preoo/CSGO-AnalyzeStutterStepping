using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using DemoInfo;

/*
 * Todo list:
 * Make velocity sorting values dynamic/configurable
 */
namespace csgo_demo_movmentstep_analyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-help")
            {
                DisplayHelp();
                return;
            }

            /*
             * One argument per demofile, thus we iterate over all of them
             * > generator.exe demo1.dem demo2.dem
             */
            foreach (var fileName in args)
            {
                /*
                 * Initializing DemoParser with fileStream needs to be done
                 * with "using". Orig.Author warned about memoryleaks if we 
                 * do not dispose fileStream / DemoParser.
                 */
                using (var fileStream = File.OpenRead(fileName))
                {
                    Console.WriteLine("Beginning parsing of " +fileName);
                    using (var parser = new DemoParser(fileStream)){
                        /*
                         * After initialization, always parse demoheader
                         * Create outPutFile&Stream
                         * Create CSVHeader for outPutFile
                         */
                        parser.ParseHeader();
                        DateTime timenow = DateTime.Now;
                        
                        string map = parser.Map;
                        string outPutFileName = timenow.ToString("d") + "." + map + ".csv";
                        var outPutStream = new StreamWriter(outPutFileName);
                        outPutStream.WriteLine(GenerateCSVHeader());

                        /*
                         * Let's get srated on analyzing demo
                         * According to DemoInfo author, CSGO
                         * is mainly based on events. Therefore 
                         * we use binding and event driven programing
                         * to do our bidding.
                         */

                        bool hasMatchStarted = false;
                        List<Player> ingame = new List<Player>();
                        Dictionary<Player, List<double>> dicPlayerFiredVelocity = new Dictionary<Player, List<double>>();
                        parser.MatchStarted += (sender, e) =>
                        {
                            hasMatchStarted = true;
                            ingame.AddRange(parser.PlayingParticipants);
                        };

                        parser.RoundStart += (sender, e) =>
                        {
                            if (!hasMatchStarted) { return; }
                            dicPlayerFiredVelocity.Clear();

                        };
                        /*
                         * Event WeaponFired has 2 parameters,
                         * Shooter(Player) & Weapon(Weapon)
                         */
                        parser.WeaponFired += (sender, e) =>
                        {
                            if (hasMatchStarted)
                            {
                                if (dicPlayerFiredVelocity.ContainsKey(e.Shooter))
                                {
                                    dicPlayerFiredVelocity[e.Shooter].Add(e.Shooter.Velocity.Absolute);
                                    
                                }
                                else
                                {
                                    dicPlayerFiredVelocity.Add(e.Shooter, new List<double> { e.Shooter.Velocity.Absolute });
                                }
                            }
                        };

                        /*
                         * On roundEnd, print information to 
                         * outputstream
                         */
                        parser.RoundEnd += (sender, e) =>
                        {
                            if (!hasMatchStarted) { return; }
                            OutPutStreamRoundResults(parser, outPutStream, dicPlayerFiredVelocity);
                        };

                        /*
                         * Call this after binding events
                         * Parses demofile to the end.
                         * Remember to close StreamWriter.
                         */

                        parser.ParseToEnd();
                        outPutStream.Close();
                    }
                }
            }

        }
        static void DisplayHelp()
        {
            string fileName = Path.GetFileName((Assembly.GetExecutingAssembly().Location));
            Console.WriteLine("Based on: CS:GO Demo-Statistics-Generator by:");
            Console.WriteLine("http://github.com/moritzuehling/demostatistics-creator");
            Console.WriteLine("Modified to Show Velocity on FireweaponEvent");
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Usage: {0} [--help] file1.dem [file2.dem ...]", fileName);
            Console.WriteLine("--help");
            Console.WriteLine("    Displays this help");

            Console.WriteLine("file1.dem");
            Console.WriteLine("    Path to a demo to be parsed. The resulting file with have the same name, ");
            Console.WriteLine("    except that it'll end with \".dem.[map].csv\", where [map] is the map.");
            Console.WriteLine("    The resulting file will be a CSV-File containing some statistics generated");
            Console.WriteLine("    by this program, and can be viewed with (for example) LibreOffice");
            Console.WriteLine("[file2.dem ...]");
            Console.WriteLine("    You can specify more than one file at a time.");
        }
        static string GenerateCSVHeader()
        {
            return string.Format(
                    "{0};{1};{2};{3};{4};{5};",
                    "Round",
                    "PlayerName",
                    "Velocity:0-20",
                    "Velocity:21-50",
                    "Velocity:51-70",
                    "Velocity:>70"
                );
        }
        /*
         * For now, do everything here
         * and send directly to streamwriter
         */
        static void OutPutStreamRoundResults(DemoParser parser, StreamWriter outPutStream, Dictionary<Player,List<double>> dicPlayerVelocity)
        {
            string roundsPlayed = string.Format("{0}", parser.TScore + parser.CTScore);
            int vel020=0, vel2150=0, vel5170=0, vel71=0;
            foreach (var player in dicPlayerVelocity)
            {
                string playerName = player.Key.Name;
                player.Value.ForEach(delegate(double vel){
                    if (vel >= 0 && vel < 21)
                    {
                        vel020++;
                    } 
                    if (vel >= 21 && vel < 51)
                    {
                        vel2150++;
                    }
                    if (vel >= 51 && vel < 71)
                    {
                        vel5170++;
                    }
                    if (vel >= 71)
                    {
                        vel71++;
                    }
                });

                /*
                 * We have everything.
                 * Time to create new line in .csv
                 */
                outPutStream.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};",
                    roundsPlayed,
                    playerName,
                    vel020,
                    vel2150,
                    vel5170,
                    vel71));
            }

        }


    }
}

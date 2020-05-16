using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Grab the pellets as fast as you can!
 **/
class Player
{
    const int SUPER_PELLET_VALUE = 7;
    const int NUM_PARTITIONS = 6;
    const int STATUS_AVAILABLE = -1;
    const int STATUS_BUSY = 0;
    const int STATUS_COMPLETE = 1;

    static List<string> Map = new List<string>();
    static int[] PelletsSubParts;
    static int[,] PartitionMap;
    static Dictionary<int, int[]> PacMen = new Dictionary<int, int[]>();
    static Dictionary<int, int[]> PacMenTargets = new Dictionary<int, int[]>();
    static Dictionary<int, int[]> EnemyPacMen = new Dictionary<int, int[]>();
    static Dictionary<int, List<int[]>> Pellets = new Dictionary<int, List<int[]>>();
    static List<int> PartitionStatus = new List<int>();

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)
        
        PartitionMap = new int[height, width];
        for (int y = 0; y < height; y++)
        {
             Map.Add(Console.ReadLine());  // one line of the grid: space " " is floor, pound "#" is wall
            
            var segmentWidth = width/3;
            for(var x = 0; x < width; x++) {
                var subParX = x < segmentWidth ? 0: x < segmentWidth*2 ? 1:2; 
                var subParY = y < height/2 ? 0:3;
                var subPartition = subParX + subParY;
                
                PartitionMap[y,x] = subPartition;
            }
        }

        // initialize Pellet Map with empty Lists for pellet location
        // and set all partitions to available
        for(var i = 0; i < 6; i++) {
            Pellets.Add(i, new List<int[]>());
            PartitionStatus.Add(STATUS_AVAILABLE);
        }

        var initialized = false;

        // game loop
        while (true)
        {
            inputs = Console.ReadLine().Split(' ');
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            for (int i = 0; i < visiblePacCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int pacId = int.Parse(inputs[0]); // pac number (unique within a team)
                bool mine = inputs[1] != "0"; // true if this pac is yours
                int x = int.Parse(inputs[2]); // position in the grid
                int y = int.Parse(inputs[3]); // position in the grid
                string typeId = inputs[4]; // unused in wood leagues
                int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
                int abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues
            
                if(!initialized) {
                    if(mine) {
                        PacMen.Add(pacId, new int[3]{x, y, STATUS_AVAILABLE});
                        PacMenTargets.Add(pacId, null);
                    } else {
                        EnemyPacMen.Add(pacId, new int[2]{x, y});
                    }
                } else {
                    if(mine) {
                        PacMen[pacId][0] = x;
                        PacMen[pacId][1] = y;
                    } else {
                        EnemyPacMen[pacId][0] = x;
                        EnemyPacMen[pacId][1] = y;
                    }
                }

                 if(mine & PacMen[pacId][2] == STATUS_AVAILABLE) {
                    Console.Error.WriteLine(string.Format("Getting Partition for: {0}", pacId));
                    PacMen[pacId][2] = getAvailablePartition();
                }

                 if(initialized) {
                    // drop target if hit
                    if(mine && PacMenTargets[pacId] != null && PacMen[pacId][0] == PacMenTargets[pacId][0] && PacMen[pacId][1] == PacMenTargets[pacId][1]) {
                        Console.Error.WriteLine(string.Format("Target Hit: [{0},{1}]-Id: {2}", PacMen[pacId][0],PacMen[pacId][1],pacId));
                        PacMenTargets[pacId] = null;
                    }

                    // set target if empty
                    // todo: reintroduce
                    if(mine && PacMenTargets[pacId] == null) {
                        Console.Error.WriteLine("Getting Pellet "+pacId);
                        PacMenTargets[pacId] = getClosestSubpartitionPellet(PacMen[pacId]);
                        // var pelletFound = getClosestSubpartitionPellet(PacMen[pacId]);
                        if(PacMenTargets[pacId] == null) {
                            Console.Error.WriteLine(string.Format("Partition Complete: {0}", PacMen[pacId][2]));
                            if(PacMen[pacId][2] >= 0) {
                                PartitionStatus[PacMen[pacId][2]] = STATUS_COMPLETE;
                            }
                            PacMen[pacId][2] = STATUS_AVAILABLE;
                        }
                        Console.Error.WriteLine(string.Format("Closest Pellet Found: {0}", PacMenTargets[pacId] != null ? PacMenTargets[pacId][0].ToString():"Error"));
                    } 
                }
            }
            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            var pelletPartition = 0;
            for (int i = 0; i < visiblePelletCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth
            
                if(!initialized){
                    pelletPartition = PartitionMap[y,x];
                
                    Pellets[pelletPartition].Add(new int[3]{x, y, value == 1 ? 1 : SUPER_PELLET_VALUE});
                    Console.Error.WriteLine(string.Format("Adding Pellet[{0}]: [{1},{2}]: {3}", pelletPartition, x, y, value));
                }
                    
            }
            initialized = true;
            
            var action = string.Empty;
            foreach(var pacManTarget in PacMenTargets) {
                if(pacManTarget.Value != null) {
                    action += string.Format("MOVE {0} {1} {2}|", pacManTarget.Key, pacManTarget.Value[0], pacManTarget.Value[1]);
                }    
            }
            action = string.IsNullOrEmpty(action)? "MOVE 0 15 10" : action.TrimEnd('|');
            // Console.Error.WriteLine(string.Format("Sending Action: {0}", action));
            Console.WriteLine(action);

        }
    }

     static int getAvailablePartition() {
        for(var i = 0; i < NUM_PARTITIONS; i++) {
            if(PartitionStatus[i] == STATUS_AVAILABLE) {
                PartitionStatus[i] = STATUS_BUSY;
                return i;
            }
        }
        return STATUS_AVAILABLE;
    }

    static int[] getClosestSubpartitionPellet(int[] pacMan) {
        // Console.Error.WriteLine(string.Format("Getting Pellet Location: [{0},{1},{2}]", pacMan[0],pacMan[1],pacMan[2]));
        
        if(pacMan[2] == STATUS_AVAILABLE) {
            return null;
        }
        
        var maxValue = int.MinValue;
        var currentValue = int.MinValue;
        int[] maxPellet = null;
        foreach(var pellet in Pellets[pacMan[2]]) {
            
            currentValue = getLocationValue(pacMan, pellet);
            // Console.Error.WriteLine(string.Format("Max: {0} => Compare Pellet: [{1},{2},{3}] - {4} ",maxValue,pellet[0],pellet[1],pellet[2],currentValue));
            if(currentValue > maxValue) {
                maxValue = currentValue;
                maxPellet =  pellet;
                // Console.Error.WriteLine(string.Format("Max Pellet updated: [{0},{1}]-{2}", maxPellet[0], maxPellet[1], maxValue));
            }
        }
        Pellets[pacMan[2]].Remove(maxPellet);
        
        return maxPellet;
    }

    static int getLocationValue(int[] pacMan, int[] pellet) {
        // already adjusted for Super Pellet Weight, use directly
        return pellet[2] - distance(pacMan, pellet);
    }

    static int distance(int[] pacMan, int[] pellet) {
        return Math.Abs(pellet[0]-pacMan[0]) + Math.Abs(pellet[1]-pacMan[1]);
    }
}
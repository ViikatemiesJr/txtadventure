using System;//
using System.Collections.Generic;//
using System.Diagnostics.Eventing.Reader;
using System.IO;//
using System.Linq;//
using System.Text;//
using System.Threading.Tasks;//

namespace txtadventure
{
    internal class Program
    {
        static void Main(string[] args)
        {
            winSize(); checkDirectory(); prepareTempFiles();
            int valinta; bool Quit = true;
            valinta = continueSavedGame();
            int[] rawChar = new int[7]; // 0 = Gender {0 male, 1 female}, 1 = Class {0 warrior, 1 rogue, 2 merchant}, 2 = #Str, 3 = #Agi, 4 = #Char, 5 = #Endu, 6 = #Luck
            int[] inventory = new int[13];
            int[] timeLocat = new int[3];
            if (valinta != 1)
            {
                prepareSaveFile();
                int igTime = 1200; int location = 5; int day = 1;
                rawChar = createChar(rawChar);
                inventory = startInventory(rawChar, inventory);
                timeLocat[0] = igTime; timeLocat[1] = location; timeLocat[2] = day;
                writeSaveFile(rawChar, inventory, timeLocat);
            }// create new char
            else
                readSaveFile(rawChar, inventory, timeLocat);
            while (Quit)
            {
                printHUD(rawChar, inventory, timeLocat);
                locationDescriptions(timeLocat[1]);
                valinta = valintaRak(timeLocat, rawChar, inventory); // Valinta indeksi
                if (valinta == -1)
                {
                    Quit = false;
                    writeSaveFile(rawChar, inventory, timeLocat);
                }// Quit käsittely
                else if (valinta == -2)
                {
                    openInventory(rawChar, inventory, timeLocat);
                }// Inventory
                else if (valinta == -3)
                {
                    Quit = openDebugMenu(rawChar, inventory, timeLocat);
                }// Hidden DeBug Menu, open by inputting "tes"
                else
                {
                    Console.Clear();
                    printHUD(rawChar, inventory, timeLocat);
                    Quit = eventHandler(rawChar, inventory, timeLocat, valinta);
                    if (Quit == false)
                        writeSaveFile(rawChar, inventory, timeLocat);
                    Console.Clear();
                }// Events
            }
            File.Delete("C:\\temp\\txtadventure\\Weaps.txt");
            File.Delete("C:\\temp\\txtadventure\\Items.txt");
            Console.WriteLine("\n  Closing the game, press *Enter*");
            Console.ReadLine();
        }
        /* rawChar ID Muistio:
         *      txt Line ID = 0
         *      slot 0 - Gender {0 male, 1 female}
         *      slot 1 - Class {0 warrior, 1 rogue, 2 merchant}
         *      slot 2 to 6 - Attribuutit
         *      2 = #Str, 3 = #Agi, 4 = #Char, 5 = #Endu, 6 = #Luck
         */
        /* inventory ID Muistio:
         *      txt Line ID = 1
         *      Huom: jos slot ID == 0 niin se on tyhjä.
         *      Huom: Item/ Aseen Max nimen pituus = 11 merkkiä
         *      
         *      slot 0 - Raha; slot 1 - HP; slot 2 - Hevonen; slot 3 - Ase
         *      
         *      //Item edit = prepareTempFiles(), getItemTxt()
         *      slot 4 -> raja; Muut tavarat
         *      
         *      4 = rabbitfoot, 5 = baby, 6 = ration, 7 = beer, 8 = wildpotion, 9 = cure potion, 10 = poison, 11 = lockpick, 12 = hay,
         *      13 = torch, 14 = hp potion (minor), 15 = hp potion (major), 16 = skillup potion(timed), 17 = evolution potion (perm),
         *      18 = illeagal narcotics, 19 = vampire ash, 20 = voxor ore
         *      
         *      
         *      //Ase edit = prepareTempFiles(), getWeapTxt(),
         *      Aseet t1: 2H Axe = 1; Dagger = 2; Short Sword = 3
         *      Aseet t2: Greataxe = 4; Tanto = 5; Scimitar = 6;
         *      Aseet t3: Voxe = 7; Vogger = 8; Voxord = 9;
         *      
         *      Hevonen: Kyllä = 1
         */
        /* location ID Muistio:
         *      location biggest id = 34
         *      Fel Fain:
         *      5 = Alku Barn (Fain), 6 = main street (Fain), 7 = inn (Fain), 8 = smith (Fain), 9 = stable (Fain), 10 = generalstore (Fain), 
         *      11 = shrine (Fain), 12 = back alley (Fain), 13 = castle (Fain), 14 = prison (Fain)
         *      
         *      Fel Mara:
         *      15 = main street (Mara), 16 = inn (Mara), 17 = smith (Mara), 18 = stable (Mara), 19 = generalstore (Mara), 20 = alchemist (Mara)
         *      21 = temple (Mara), 22 = arena (Mara), 23 = slavemarket (Mara), 24 = monastery (Mara), 25 = back alley (Mara), 26 = marketsquare (Mara)
         *      27 = castle (Mara), 28 = prison (Mara)
         *      
         *      Road:
         *      29 = cave (road), 30 = crossroads (road), 31 = inn (road), 32 = ruins (road), 33 = mines (road), 34 = mountains (road)
         *      
         *      road: Fel Fain - Cave(smuglers) - Crossroads(theft) - Inn(road) - Ruins(vampires) - Mines(voxor ore) - Fel Mara
         *      map: (endpoint)                          -                                                           (endpoint)
         *      here:                             Mountains(treasure)
         */
        /* Status effects ID Muistio:
         *      txt Line ID = 3
         *      0 = Rabbits Foot (0/1)
         *      1 = Major STD (-1/0/1) {-1 active (old endurance), 0 no, 1 hidden(day caught)} activate if "day caught" + 2 == "current day"
         *      2 = Karma ( 0 -+ any)
         *      3 = Wanted (0 ->) {0-25 = no, 25-99 = fine, 100-499 = jail, 500 = death penalty}
         *      4 = Drunk (0/1->) // alc = +str +2 cha - agi
         *      5 = Pregnant (-1/0->90) {-1 na, 0-5 none, 6-30 minor, 31-60 medium, 61-85 major, 86-90 extreme}
         *      6 = Hungry (0 ->) {days since last meal}
         *      7 = Hangover (0/1) // hangover = -str -cha -agi
         *      8 = skillup (0/day)
         *      9 = skillup skill (0/2->6)
         *      10 = amount of sex/ times used threat with violence
         *      11 = Mountain info (0/1)
         *      12 = Horse exhaustion (0->3) {return 1 for 3h sleep, 3 for hay; if @ 3 = negates horse speed buff}
         *      13 = Hungry baby (0 ->) {days since last meal}
         *      14 = pregnancy speed modifier (1->5) {na/none = 1, minor = 2, medium = 3, major = 4, extreme = 5}
         */
        /* Things to Add later:
         *      Skipped due to lack of mechanics or came in mind when doing smt else
         *      
         *      Drunken sex @ openInventor(), case 2, case 7
         *      inventory wild potion @ openInventory, case 2, case 8
         */
        // File handling
        static void winSize()
        {
            try
            {
                try { Console.SetWindowSize(160, 60); }
                catch { Console.SetWindowSize(160, 40); Console.WriteLine("\nCould not automatically resize console window to optimal size.\n\nPress *Enter* to start the game."); Console.ReadLine(); Console.Clear(); }
            }
            catch
            {
                Console.WriteLine("\nCould not automatically resize console window to optimal size.\nYou might need to resize it manually at some point.\n\nPress *Enter* to start the game.");
                Console.ReadLine(); Console.Clear();
            }
        }
        static void checkDirectory()
        {
            if (!Directory.Exists("C:\\temp"))
                Directory.CreateDirectory("C:\\temp");
            if (!Directory.Exists("C:\\temp\\txtadventure"))
                Directory.CreateDirectory("C:\\temp\\txtadventure");
        }
        static void prepareTempFiles()
        {
            if (File.Exists("C:\\temp\\txtadventure\\Weaps.txt"))
                File.Delete("C:\\temp\\txtadventure\\Weaps.txt");
            int rivit = 10;// väh. weps (isoin id +1)
            int[] temp = new int[rivit]; 
            int[] weps = new int[4];  // muutos also -> readLineFromWeps()
            string[] template = new string[rivit]; 
            for (int i = 0; i < template.Length; i++)
                template[i] = Convert.ToString(temp[i]);
            File.WriteAllLines("C:\\temp\\txtadventure\\Weaps.txt", template);
            //      dmg,        value,      str req,       heavy? (0/1)
            weps[0] = 0; weps[1] = 0; weps[2] = 0; weps[3] = 0; writeLineToWeps(weps, 0); // Nyrkit
            // T1
            weps[0] = 3; weps[1] = 50; weps[2] = 3; weps[3] = 1; writeLineToWeps(weps, 1); // 2H Axe
            weps[0] = 1; weps[1] = 10; weps[2] = 0; weps[3] = 0; writeLineToWeps(weps, 2); // Dagger
            weps[0] = 2; weps[1] = 50; weps[2] = 0; weps[3] = 0; writeLineToWeps(weps, 3); // Short Sword
            // T2
            weps[0] = 5; weps[1] = 100; weps[2] = 4; weps[3] = 1; writeLineToWeps(weps, 4); // Greataxe
            weps[0] = 3; weps[1] = 100; weps[2] = 0; weps[3] = 0; writeLineToWeps(weps, 5); // Tanto
            weps[0] = 4; weps[1] = 150; weps[2] = 1; weps[3] = 0; writeLineToWeps(weps, 6); // Scimitar
            // T3
            weps[0] = 7; weps[1] = 500; weps[2] = 5; weps[3] = 1; writeLineToWeps(weps, 7); // Voxe
            weps[0] = 5; weps[1] = 500; weps[2] = 1; weps[3] = 0; writeLineToWeps(weps, 8); // Vogger
            weps[0] = 6; weps[1] = 750; weps[2] = 2; weps[3] = 0; writeLineToWeps(weps, 9); // Voxord
            //weps[0] = ; weps[1] = ; weps[2] = ; weps[3] = ; writeLineToSVFile(weps, );
            if (File.Exists("C:\\temp\\txtadventure\\Items.txt"))
                File.Delete("C:\\temp\\txtadventure\\Items.txt");
            rivit = 21; // väh. items (isoin id +1)
            int[] temp2 = new int[rivit];
            int[] items = new int[4]; // muutos also -> readLineFromItems()
            string[] template2 = new string[rivit];
            for (int i = 0; i < template2.Length; i++)
                template2[i] = Convert.ToString(temp2[i]);
            File.WriteAllLines("C:\\temp\\txtadventure\\Items.txt", template2);
            //      value, discardable (0/1), illeagal (0/1), usable (0/1)        alken slot 4
            items[0] = 50;  items[1] = 1; items[2] = 0; items[3] = 0; writeLineToItems(items, 4);// rabbitfoot
            items[0] = 100; items[1] = 0; items[2] = 0; items[3] = 1; writeLineToItems(items, 5);// baby
            items[0] = 2;   items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 6);// ration
            items[0] = 5;   items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 7);// beer 
            items[0] = 25;  items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 8);// wildpotion
            items[0] = 100; items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 9);// cure potion
            items[0] = 25;  items[1] = 1; items[2] = 1; items[3] = 0; writeLineToItems(items, 10);// poison
            items[0] = 25;  items[1] = 1; items[2] = 1; items[3] = 0; writeLineToItems(items, 11);// lockpick
            items[0] = 10;  items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 12);// hay
            items[0] = 10;  items[1] = 1; items[2] = 0; items[3] = 0; writeLineToItems(items, 13);// torch
            items[0] = 15;  items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 14);// hp potion minor
            items[0] = 60;  items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 15);// hp potion major
            items[0] = 30;  items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 16);// skillup potion timed
            items[0] = 250; items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 17);// evolution potion perm
            items[0] = 100; items[1] = 1; items[2] = 1; items[3] = 0; writeLineToItems(items, 18);// narcotics
            items[0] = 100; items[1] = 1; items[2] = 0; items[3] = 0; writeLineToItems(items, 19);// vampire ash
            items[0] = 100; items[1] = 1; items[2] = 0; items[3] = 0; writeLineToItems(items, 20);// voxor ore
            //      value,          blank,         blank,       blank.
            items[0] = 100; items[1] = 0; items[2] = 0; items[3] = 0; writeLineToItems(items, 2);// Horse

        }// Item ja Weaps statit
        static void prepareSaveFile()
        {
            if (!File.Exists("C:\\temp\\txtadventure\\SVfile.txt"))
            {
                int rivit = 35; // väh isoin location id + 1
                int[] temp = new int[rivit];
                int[] status = { 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
                string[] template = new string[rivit];
                for (int i = 0; i < template.Length; i++)
                    template[i] = Convert.ToString(temp[i]);                
                File.WriteAllLines("C:\\temp\\txtadventure\\SVfile.txt", template);
                writeLineToSVFile(status, 3);
            }            
        }
        static int continueSavedGame()
        {
            int value = 0;
            if (File.Exists("C:\\temp\\txtadventure\\SVfile.txt"))
            {
                string[] svFile = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
                if (svFile[0] == "0") { File.Delete("C:\\temp\\txtadventure\\SVfile.txt"); value = -2; }
                else if (readSlotFromSVFile(1, 1) == 0) { File.Delete("C:\\temp\\txtadventure\\SVfile.txt"); value = -3; }
                else
                {
                    bool cont = true;
                    while (cont)
                    {
                        Console.WriteLine("\n  Welcome to txt adventure game.\n\n  WARNING! This game contains descriptions of events that can trigger old traumas.\n  Violence, Sex, Drugs, Gambling, Human Trafficking\n  Play at your own risk. You have been warned.\n\n  Save file detected\n  Do you want to continue from it, or start fresh?\n\n  Type Y or y for continue, N or n for new game");
                        switch (Console.ReadLine())
                        {
                            case "Y":
                            case "y":
                            case "1":
                                value = 1; cont = false;
                                break;
                            case "N":
                            case "n":
                            case "2":
                                Console.WriteLine("Creating new file will permanently delete your old savefile\nDo you want to proceed?\n\nType F to proceed, to Cancel type anything else");
                                switch (Console.ReadLine())
                                {
                                    case "F":
                                    case "f":
                                        File.Delete("C:\\temp\\txtadventure\\SVfile.txt");
                                        value = -1; cont = false;
                                        break;
                                }
                                break;
                            default:
                                Console.WriteLine("Invalid selection, press *Enter* and try again");
                                Console.ReadLine();
                                break;
                        }
                        Console.Clear();
                    }
                }
            }
            return value;
        }// 0 = no sv, -1 = ng, 1 = continue
        static void writeSaveFile(int[] rawChar, int[] inventory, int[] timeLocat)
        {
            writeLineToSVFile(rawChar, 0);
            writeLineToSVFile(inventory, 1);
            writeLineToSVFile(timeLocat, 2);
        }
        static void writeLineToSVFile(int[] input, int id)
        {
            string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
            string write = "";
            for (int i = 0; i < input.Length; i++)
                write += input[i] + "!";
            write += "#";
            lines[id] = write;
            File.WriteAllLines("C:\\temp\\txtadventure\\SVfile.txt", lines);
        }
        static void writeSlotToSVFile(int lineID, int slot, int nroToSave)
        {
            try
            {
                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
                string line = lines[lineID];
                int j = 0; string value; bool cont = true; int lenght = 0;
                while (cont)
                {
                    if (line[j] == '!')
                        lenght++;
                    else if (line[j] == '#')
                        cont = false;
                    j++;
                }
                cont = true; j = 0;
                int[] writeline = new int[lenght];
                for (int i = 0; i < lenght; i++)
                {
                    value = ""; cont = true;
                    while (cont)
                    {
                        if (line[j] == '!')
                        { writeline[i] = Convert.ToInt32(value); cont = false; }
                        else if (line[j] == '-')
                            value += "-";
                        else
                            value += Convert.ToString(line[j]);
                        j++;
                    }
                }
                writeline[slot] = nroToSave;
                string write = "";
                for (int i = 0; i < lenght; i++)
                    write += writeline[i] + "!";
                write += "#";
                lines[lineID] = write;
                File.WriteAllLines("C:\\temp\\txtadventure\\SVfile.txt", lines);
            }
            catch
            {
                Console.WriteLine("ERROR: Failure to read or write, press *Enter* to proceed");
                Console.ReadLine();
            }
        }
        static void writeLineToWeps(int[] input, int id)
        {
            string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\Weaps.txt");
            string write = "";
            for (int i = 0; i < input.Length; i++)
                write += input[i] + "!";
            write += "#";
            lines[id] = write;
            File.WriteAllLines("C:\\temp\\txtadventure\\Weaps.txt", lines);
        }
        static void writeLineToItems(int[] input, int id)
        {
            string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\Items.txt");
            string write = "";
            for (int i = 0; i < input.Length; i++)
                write += input[i] + "!";
            write += "#";
            lines[id] = write;
            File.WriteAllLines("C:\\temp\\txtadventure\\Items.txt", lines);
        }
        static void readSaveFile(int[] rawChar, int[] inventory, int[] timeLocat)
        {
            try
            {
                readLineFromSVFile(rawChar, 0);
                readLineFromSVFile(inventory, 1);
                readLineFromSVFile(timeLocat, 2);
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to read savefile, press *Enter* to proceed, readSaveFile()");
                Console.ReadLine();
            }
        }
        static void readLineFromSVFile(int[] output, int id)
        {
            try
            {
                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
                string line = lines[id];
                int j = 0; string value; bool cont;
                for (int i = 0; i < output.Length; i++)
                {
                    value = ""; cont = true;
                    while (cont)
                    {
                        if (line[j] == '!')
                        { output[i] = Convert.ToInt32(value); cont = false; }
                        else if (line[j] == '-')
                            value += "-";
                        else
                            value += Convert.ToString(line[j]);
                        j++;
                    }
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to read line ID:" + id + " from savefile, press *Enter* to proceed, readLineFromSVFile");
                Console.ReadLine();
            }
        }
        static int readSlotFromSVFile(int lineID, int slot)
        {
            int output = 0;
            try
            {
                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
                string line = lines[lineID];
                int j = 0; string value; bool cont;
                for (int i = 0; i <= slot; i++)
                {
                    value = ""; cont = true;
                    while (cont)
                    {
                        if (line[j] == '!')
                        { output = Convert.ToInt32(value); cont = false; }
                        else if (line[j] == '-')
                            value += "-";
                        else if (line[j] == '#')
                        { Console.WriteLine("Too large slot number, press *Enter* to proceed"); Console.ReadLine(); output = 0; cont = false; }
                        else
                            value += Convert.ToString(line[j]);
                        j++;
                    }
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to read value LineID:" + lineID + " Slot:" + slot + " from savefile, press *Enter* to proceed, readSlotFromSVFile");
                Console.ReadLine();
            }
            return output;
        }
        static int[] readLineFromWeps(int id)
        {
            int[] output = new int[4];
            try
            {                
                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\Weps.txt");
                string line = lines[id];
                int j = 0; string value; bool cont;
                for (int i = 0; i < output.Length; i++)
                {
                    value = ""; cont = true;
                    while (cont)
                    {
                        if (line[j] == '!')
                        { output[i] = Convert.ToInt32(value); cont = false; }
                        else if (line[j] == '-')
                            value += "-";
                        else
                            value += Convert.ToString(line[j]);
                        j++;
                    }
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to read line ID:" + id + " from ´Weps.txt, press *Enter* to proceed, readLineFromWeps()");
                Console.ReadLine();
            }
            return output;
        }
        static int[] readLineFromItems(int id)
        {
            int[] output = new int[4];
            try
            {
                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\Items.txt");
                string line = lines[id];
                int j = 0; string value; bool cont;
                for (int i = 0; i < output.Length; i++)
                {
                    value = ""; cont = true;
                    while (cont)
                    {
                        if (line[j] == '!')
                        { output[i] = Convert.ToInt32(value); cont = false; }
                        else if (line[j] == '-')
                            value += "-";
                        else
                            value += Convert.ToString(line[j]);
                        j++;
                    }
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to read line ID:" + id + " from Items.txt, press *Enter* to proceed, readLineFromItems()");
                Console.ReadLine();
            }
            return output;
        }
        static int[] readLineFromSVFileForEvent(int lineID)
        {
            int[] output = new int[5]; // if change, edit also locationDescriptions()
            try
            {
                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
                string line = lines[lineID];
                int j = 0; string value; bool cont;
                for (int i = 0; i < output.Length; i++)
                {
                    value = ""; cont = true;
                    while (cont)
                    {
                        if (line[j] == '!')
                        { output[i] = Convert.ToInt32(value); cont = false; }
                        else if (line[j] == '-')
                            value += "-";
                        else
                            value += Convert.ToString(line[j]);
                        j++;
                    }
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to read line ID:" + lineID + " from ´SVFile.txt, press *Enter* to proceed, readLineFromSVFileForEvent()");
                Console.ReadLine();
            }
            return output;
        }
        static bool checkIfLineExistsInSVFile(int lineID)
        {
            bool output = true;
            string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
            string line = lines[lineID];
            if (line == "0")
                output = false;
            return output;
        }
        // Huds and Starting Text
        static void printHUD(int[] rawChar, int[] inventory, int[] timeLocat)
        {
            const int hpmulti = 2;
            Console.Clear(); // W_Size = 160 * 60
            Console.Write("\n  ");
            for (int i = 0; i < 94; i++)
                Console.Write("-");
            Console.WriteLine("\n{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{2,-15}|{3,-15}|{4,-15}|", null, null, "1", "2", "3", null, null);
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{0,2}{2,-13}|{0,2}{3,-13}|{0,2}{4,-13}|{0,2}{7}", null, "Gender:", getItemTxt(inventory, 4), getItemTxt(inventory, 5), getItemTxt(inventory, 6), "Attributes:", "Time: " + updateTimeAndReturnTxt(timeLocat, 0, rawChar, inventory), "For navigation use:");
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{0,2}{2,-13}|{0,2}{3,-13}|{0,2}{4,-13}|{0,2}{7}", null, getGenderTxt(rawChar), null, null, null, "Str  = " + rawChar[2], "Day: " + timeLocat[2], "Numbers or Letters show next to choice");
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{2,-15}|{3,-15}|{4,-15}|{0,2}", null, null, "---------------", "---------------", "---------------", "Agi  = " + rawChar[3], "Horse: " + isHorseTxt(inventory));
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{2,-15}|{3,-15}|{4,-15}|", null, "Skill:", "4", "5", "6", "Char = " + rawChar[4], null);
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{0,2}{2,-13}|{0,2}{3,-13}|{0,2}{4,-13}|", null, getSkillTxt(rawChar), getItemTxt(inventory, 7), getItemTxt(inventory, 8), getItemTxt(inventory, 9), "Endu = " + rawChar[5], "Weapon:");
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{0,2}{2,-13}|{0,2}{3,-13}|{0,2}{4,-13}|", null, null, null, null, null, "Luck = " + rawChar[6], getWeapTxt(inventory));
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{2,-15}|{3,-15}|{4,-15}|", null, "Class:", "---------------", "---------------", "---------------", null, null);
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{2,-15}|{3,-15}|{4,-15}|", null, getClassTxt(rawChar), "7", "8", "9", "Health:", "Gold:");
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{0,2}{2,-13}|{0,2}{3,-13}|{0,2}{4,-13}|", null, null, getItemTxt(inventory, 10), getItemTxt(inventory, 11), getItemTxt(inventory, 12), inventory[1] + " / " + rawChar[5] * hpmulti, inventory[0]);
            Console.WriteLine("{0,2}|{0,2}{1,-11}|{0,2}{5,-12}|{0,2}{6,-13}|{0,2}{2,-13}|{0,2}{3,-13}|{0,2}{4,-13}|", null, null, null, null, null, null, null);
            Console.Write("  ");
            for (int i = 0; i < 94; i++)
                Console.Write("-");
            Console.WriteLine("\n");
        }
        static void createCharTxt()
        {
            Console.WriteLine("\n  Welcome to txt adventure game.\n\n  WARNING! This game contains descriptions of events that can trigger old traumas.\n  Violence, Sex, Drugs, Gambling, Human Trafficking\n  Play at your own risk. You have been warned.\n\n  To start the game you'll need to create character by choosing gender and class\n  On the chart below you can see all choices\n");
            Console.WriteLine("{0,2}{1}{1}{2}", null, "---------------------", "-");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-15}{3}{0,2}|{0,2}{2,-15}{4}{0,2}|", null, "Gender:", "Gender:", "M", "F");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Male", "Female");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Skill:", "Skill:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Threaten", "Seduction");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Attributes:", "Attributes:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Str: +1", "Agi: +1");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Endu: +1", "Cha: +1");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|", null, "Agi: -1", "Str: -1");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}{1}{1}{1}{2}", null, "---------------------", "-");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-15}{4}{0,2}|{0,2}{2,-15}{5}{0,2}|{0,2}{3,-15}{6}{0,2}|", null, "Class:", "Class:", "Class:", "A", "B", "C");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Warrior", "Rogue", "Merchant");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Attributes:", "Attributes:", "Attributes:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Str  : 4", "Str  : 2", "Str  : 1");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Agi  : 1", "Agi  : 4", "Agi  : 2");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Char : 2", "Char : 2", "Char : 4");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Endu : 3", "Endu : 1", "Endu : 2");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Luck : 2", "Luck : 3", "Luck : 3");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "Starting Items:", "Starting Items:", "Starting Items:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "2H Axe", "Dagger", "Short Sword");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|{0,2}{2,-18}|{0,2}{3,-18}|", null, "25 Gold", "5 Gold", "100 Gold");
            Console.WriteLine("{0,2}|{0,20}|{0,20}|{0,20}|", null);
            Console.WriteLine("{0,2}{1}{1}{1}{2}", null, "---------------------", "-");

        }
        static void createCharInfo()
        {
            Console.Clear();
            Console.WriteLine("\n  Both gender skills ( Threaten / Seduction ) are primarily used to bypass some situations.\n  But be careful, These can easily backfire since they may include some additional attribute skillchecks");
            Console.WriteLine("\n  Attributes:\n  Attribute numbers natural range is from 0 to 5, where 0 is bad and 5 is best. Some items and events can modify these numbers above or below that scale.");
            Console.WriteLine("  Str = Strenght, Commonly used in Combat to deal more dmg, but also in some other strenght requiring skillchecks");
            Console.WriteLine("  Agi = Agility, Commonly used to avoid combat or when trying to steal something, but also in some other agility requiring skillchecks");
            Console.WriteLine("  Char = Charisma, Commonly used when trying to get better prices or hidden info, but also in some other charisma requiring skillchecks");
            Console.WriteLine("  Endu = Endurance, Health multiplier, the higher it is the higher healthpool one has, but can be used in some other endurance requiring skillchecks");
            Console.WriteLine("  Luck = Luck, Used in skillchecks as alone or with some other attributes.");
            Console.WriteLine("\n  Press *Enter* to go back to Character creation");
            Console.ReadLine();
        }
        static void printChar(int[] rawChar)
        {
            Console.WriteLine("\n  Your character is Ready, it's stats are show below:\n");
            Console.WriteLine("  ----------------------");
            Console.WriteLine("{0,2}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Gender:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, getGenderTxt(rawChar));
            Console.WriteLine("{0,2}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Skill:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, getSkillTxt(rawChar));
            Console.WriteLine("{0,2}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Class:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, getClassTxt(rawChar));
            Console.WriteLine("{0,2}|{0,20}|", null);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Attributes:");
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Str  = " + rawChar[2]);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Agi  = " + rawChar[3]);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Char = " + rawChar[4]);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Endu = " + rawChar[5]);
            Console.WriteLine("{0,2}|{0,2}{1,-18}|", null, "Luck = " + rawChar[6]);
            Console.WriteLine("{0,2}|{0,20}|", null);
            Console.WriteLine("  ----------------------");
        }
        // getTxt
        static string getGenderTxt(int[] rawChar)
        {
            if (rawChar[0] == 1)
                return "Female";
            else if (rawChar[0] == 0)
                return "Male";
            else
                return "ERROR";
        }
        static string getSkillTxt(int[] rawChar)
        {
            if (rawChar[0] == 1)
                return "Seduction";
            else if (rawChar[0] == 0)
                return "Threaten";
            else
                return "ERROR";
        }
        static string getClassTxt(int[] rawChar)
        {
            if (rawChar[1] == 1)
                return "Rogue";
            else if (rawChar[1] == 2)
                return "Merchant";
            else if (rawChar[1] == 0)
                return "Warrior";
            else
                return "ERROR";
        }
        static string getStatTxt(int statNro)
        {//2 = #Str, 3 = #Agi, 4 = #Char, 5 = #Endu, 6 = #Luck
            switch (statNro)
            {
                case 2:
                    return "Strenght";
                case 3:
                    return "Agility";
                case 4:
                    return "Charisma";
                case 5:
                    return "Endurance";
                case 6:
                    return "Luck";
                default:
                    return "ERROR";
            }
        }
        static string getItemTxtWithJustId(int id)
        {
            string txt;
            switch (id)
            {
                case 0:
                    txt = "Empty";
                    break;
                case 4:
                    txt = "Rabbit Foot";
                    break;
                case 5:
                    txt = "Baby";
                    break;
                case 6:
                    txt = "Ration";
                    break;
                case 7:
                    txt = "Beer";
                    break;
                case 8:
                    txt = "Wild Potion";
                    break;
                case 9:
                    txt = "Cure Potion";
                    break;
                case 10:
                    txt = "Poison";
                    break;
                case 11:
                    txt = "Lockpick";
                    break;
                case 12:
                    txt = "Haybale";
                    break;
                case 13:
                    txt = "Torch";
                    break;
                case 14:
                    txt = "HP Potion";
                    break;
                case 15:
                    txt = "HP Potion +";
                    break;
                case 16:
                    txt = "Buff Potion";
                    break;
                case 17:
                    txt = "Vox Potion";
                    break;
                case 18:
                    txt = "Narcotics";
                    break;
                case 19:
                    txt = "Vampire Ash";
                    break;
                case 20:
                    txt = "Voxor Ore";
                    break;
                default:
                    txt = "ERROR";
                    break;
            }
            return txt;
        }
        static string getItemTxt(int[] inventory, int id)
        {
            string txt;
            txt = getItemTxtWithJustId(inventory[id]);
            return txt;
        }// Tavara nimet
        static string getWeapTxt(int[] inventory)
        {
            string txt;
            switch (inventory[3])
            {
                case 0:
                    txt = "Unarmed";
                    break;
                case 1:
                    txt = "2H Axe";
                    break;
                case 2:
                    txt = "Dagger";
                    break;
                case 3:
                    txt = "Short Sword";
                    break;
                case 4:
                    txt = "Greataxe";
                    break;
                case 5:
                    txt = "Tanto";
                    break;
                case 6:
                    txt = "Scimitar";
                    break;
                case 7:
                    txt = "Voxe";
                    break;
                case 8:
                    txt = "Vogger";
                    break;
                case 9:
                    txt = "Voxord";
                    break;
                default:
                    txt = "ERROR";
                    break;
            }
            return txt;
        }// Aseiden nimet
        static string isHorseTxt(int[] inventory)
        {
            if (inventory[2] == 1)
                return "Yes";
            else if (inventory[2] == 0)
                return "No";
            else
                return "ERROR";
        }
        // Basic Funcs
        static void dailyChecks(int[] rawChar, int[] inventory, int[] timeLocat)
        {
            int[] status = readLineFromSVFileForEvent(3); bool trigger = false;
            if (status[1] > 0 && status[1] + 2 >= timeLocat[2])
            {
                Console.WriteLine("\n  You start to feel radiating pain that originates from your crotch. You'v caught something undesirable." +
                    "\n  One things for sure, aslong as it is not cured, you can't regenerate health and your whole body is quite fragile");
                status[1] = 0 - rawChar[5]; rawChar[5] = 0; trigger = true;
            }// std
            if (status[7] != 0)
            {
                Console.WriteLine("Your hangover has passed and you are ready for new day.");
                status[7] = 0; rawChar[2] += 1; rawChar[3] += 1; rawChar[4] += 1; trigger = true;
            }// hangover
            if (status[4] >= 1)
            {
                Console.WriteLine("\n  Your buzz from alcohol has weared down. Now you just have headache." +
                    "\n  It'll be gone tomorrow, or maybe you could just buy a new beer");
                status[4] = 0; rawChar[2] -= 2; rawChar[4] -= 3; trigger = true;
            }// drunk            
            if (status[5] != -1)
            {
                //{ -1 na, 0 - 5 none, 6 - 30 minor, 31 - 60 medium, 61 - 85 major, 86 - 90 extreme}
                //{na/none = 1, minor = 2, medium = 3, major = 4, extreme = 5}
                bool malnutrition = false;
                if (status[5] >= 6 && status[5] <= 30 && status[14] < 2)
                {
                    Console.WriteLine("  You start to crave food more than what's normal to you." +
                          "\n  You also have some morning sickness, unusual mood swings and you feel quite tired." +
                          "\n  Hopefully it's nothing, but you also might have accidentaly gotten pregnant." +
                          "\n  Just to be safe you'd better start eating once every 3 days and avoid fights." +
                          "\n  All these mild anoyances start to affect how fast you can do some tasks.");
                    status[14] = 2;
                }// minor
                else if (status[5] >= 31 && status[5] <= 60 && status[14] < 3)
                {
                    Console.WriteLine("  During past days it's become more obvious that you are actually pregnant." +
                        "\n  Since your belly has grow, most men seem to have grown resistant to your seduction attempts." +
                        "\n  That bigger than usual belly has also made you even slower than usual");
                    rawChar[4] -= 1; rawChar[3] -= 1; // charisma , agi
                    status[14] = 3;
                }// medium
                else if (status[5] >= 61 && status[5] <= 85 && status[14] < 4)
                {
                    Console.WriteLine("  As the pregnancy goes further, you seem to stumble a lot more.");
                    rawChar[3] -= 1; // agi
                    status[14] = 4;
                }// major
                else if (status[5] >= 86 && status[5] <= 90 && status[14] < 5)
                {
                    Console.WriteLine("  As the birh draws closer, you are significantly slower and even smallest tasks seem like massive chores");
                    rawChar[3] -= 3; rawChar[4] -= 1; // agi charisma
                    status[14] = 5;
                }// extreme
                else if (status[5] >= 91)
                {
                    Console.WriteLine("  Your baby has born, and you are back to normal. Just don't forget to feed the baby once every 3 days");
                    rawChar[3] += 5; rawChar[4] += 2;
                    status[14] = 1; status[5] = -1;
                }// birth
                if ((status[5] >= 9))
                {
                    if (status[6] + 4 >= timeLocat[2])
                    {
                        malnutrition = true;
                        if (status[14] >= 3) { rawChar[3] += 1; rawChar[4] += 1; }
                        if (status[14] >= 4) rawChar[3] += 1;
                        if (status[14] >= 5) { rawChar[3] += 4; rawChar[4] += 1; }
                        status[14] = 1;
                    }// malnutrition check
                }
                if (malnutrition == false) status[5] += 1;
                else { status[5] = 0; Console.WriteLine("  You caused abortion by malnutrition"); status[2] -= 500; }
                trigger = true;
            }// pregnancy
            if (status[8] + 2 >= timeLocat[2])
            {
                Console.WriteLine("\n  Your skill potion has wore down. You have lost it's buff");
                rawChar[status[9]] -= 1; status[8] = 0; status[9] = 0; trigger = true;
            }// skillup potion            
            {
                bool rabbitIs = false;
                for (int i = 4; i > 12; i++)
                {
                    if (inventory[i] == 4) { rabbitIs = true; break; }
                }
                if (rabbitIs == true || status[0] != 1)
                {
                    Console.WriteLine("  You suddenly feel luckier. Maybe that weird rabbit trinket actually does something.");
                    status[0] = 1; rawChar[6] += 1; trigger = true;
                }
                else if (rabbitIs != true || status[0] == 1)
                {
                    Console.WriteLine("  You don't feel extra lucky anymore. Maybe that weird rabbit trinket actually did something.");
                    status[0] = 0; rawChar[6] -= 1; trigger = true;
                }
            }// rabbits foot
            if (trigger == true)
            {
                writeLineToSVFile(status, 3);
                writeSaveFile(rawChar, inventory, timeLocat);
                Console.WriteLine("\n  Press *Enter* to continue");
                Console.ReadLine();
            }
        }
        static bool openDebugMenu(int[] rawChar, int[] inventory, int[] timeLocat)
        {
            bool cont = true; int[] status = readLineFromSVFileForEvent(3); string txt;
            while (cont)
            {
                Console.Clear();
                printHUD(rawChar, inventory, timeLocat); txt = ""; int val2;
                Console.WriteLine("  This is DeBug Menu. Using this can make some unwanted stuff to happen. No confirmations in here, what you input will happen." +
                    "\n  This is not intended for regular gameplay. If you input invalid value, nothing will happen and you will be returned to this menu." +
                    "\n\n  0 to 'Exit'" +
                    "\n  1 to change 'Inventory' items including weapon and horse" +
                    "\n  2 to gain, lose or set 'Gold'" +
                    "\n  3 to gain, lose or set current 'Health'" +
                    "\n  4 to gain, lose or set 'Stats'" +
                    "\n  5 to change gender" +
                    "\n  6 to set 'Time' and 'Day'" +
                    "\n  7 to 'Teleport' to any 'Location'" +
                    "\n  8 to 'Reset' any 'Location'" +
                    "\n  9 to edit some 'Status effects'" +
                    "\n  10 to gain, lose or set 'Karma'" +
                    "\n  11 to gain, lose or set 'Bounty'");
                string val = Console.ReadLine();
                Console.Clear();
                printHUD(rawChar, inventory, timeLocat);
                switch (val)
                {
                    case "0":
                        cont = false;
                        break;// Exit
                    case "1":
                        try
                        {
                            Console.WriteLine("  1 to edit Items, 2 to change Weapon, 3 to toggle Horse.");
                            switch (Console.ReadLine())
                            {
                                case "1":
                                    Console.WriteLine("  Which inventory slot you want to edit? ( 1 to 9 )");
                                    int val3 = int.Parse(Console.ReadLine()) + 3;
                                    if (val3 >= 4 && val3 <= 12)
                                    {
                                        Console.WriteLine("  To what item you want to change this? You currently have " + getItemTxtWithJustId(val3) + " in this slot");
                                        Console.WriteLine("  0 = Empty, 1 = Rabbit Foot, 2 = Baby, 3 = Ration, 4 = Beer, 5 =  Wild Potion," +
                                            "\n  6 = Cure Potion, 7 = Poison, 8 = Lockpick, 9 = Haybale, 10 = Torch, 11 = HP Potion," +
                                            "\n  12 = HP Potion +, 13 = Buff Potion, 14 = Vox Potion, 15 = Narcotics, 16 = Vampire Ash, 17 = Voxor Ore");
                                        int val4 = int.Parse(Console.ReadLine());
                                        if (val4 == 0) { inventory[val3] = val4; txt = "  Set inventory slot " + (val3 - 3) + " as Empty."; }
                                        else if (val4 >= 1 && val4 <= 17) { inventory[val3] = val4 + 3; txt = "  Set inventory slot " + (val3 - 3) + " as " + getItemTxtWithJustId(val4) + "."; }
                                        else txt = "Invalid number";
                                    }
                                    else txt = "Invalid number";
                                    break;// edit items
                                case "2":
                                    Console.WriteLine("  Your current weapon is " + getWeapTxt(inventory));
                                    Console.WriteLine("  What weapon would you like to have?" +
                                        "\n  No weapon = 0" +
                                        "\n  TIER 1: 2H Axe = 1, Dagger = 2, Short Sword = 3" +
                                        "\n  TIER 2: Greataxe = 4, Tanto = 5, Scimitar = 6," +
                                        "\n  TIER 3: Voxe = 7, Vogger = 8, Voxord = 9.");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 >= 0 && val2 <= 9) { inventory[3] = val2; txt = "  Your wwapon was changed to " + getWeapTxt(inventory); }
                                    else txt = "Invalid number";
                                    break;// change weapon
                                case "3":
                                    if (inventory[2] == 0) { inventory[2] = 1; txt = "  You now have a horse."; }
                                    else { inventory[2] = 0; txt = "  You lost your horse."; }
                                    break;// horse toggle
                                default:
                                    txt = "Invalid number";
                                    break;
                            }
                        }
                        catch { txt = "Invalid number."; }
                        break;// Inventory
                    case "2":
                        try
                        {
                            Console.WriteLine("  1 for addition or subtraction of Gold, 2 to set specific value to Gold.");
                            val2 = int.Parse(Console.ReadLine()); int val3;
                            if (val2 == 1)
                            {
                                Console.WriteLine("  How much Gold you want to add or substract. Use negative number for substraction.");
                                val3 = int.Parse(Console.ReadLine());
                                inventory[0] += val3;
                            }
                            else if (val2 == 2)
                            {
                                Console.WriteLine("  What value you want your Gold to be.");
                                val3 = int.Parse(Console.ReadLine());
                                inventory[0] = val3;
                            }
                            else { txt = "Invalid number."; break; }
                            if (inventory[0] < 0) inventory[0] = 0;
                            if (txt == "") txt = "  Your Gold amount was set to " + inventory[0];
                        }
                        catch { txt = "Invalid number."; }
                        break;// Gold
                    case "3":
                        try
                        {
                            Console.WriteLine("  1 for addition or subtraction of Health, 2 to set specific value to Health.");
                            val2 = int.Parse(Console.ReadLine()); int val3;
                            if (val2 == 1)
                            {
                                Console.WriteLine("  How much Health you want to add or substract. Use negative number for substraction.");
                                val3 = int.Parse(Console.ReadLine());
                                inventory[1] += val3;
                            }
                            else if (val2 == 2)
                            {
                                Console.WriteLine("  What value you want your Health to be.");
                                val3 = int.Parse(Console.ReadLine());
                                inventory[1] = val3;
                            }
                            else { txt = "Invalid number."; break; }
                            if (inventory[1] <= 0)
                            {
                                Console.WriteLine("  WARNING! You set your Health to or below 0. Input Y or y to proceed. This WILL kill you.\n  Input anything else to be left at 1 Health.");
                                switch (Console.ReadLine())
                                {
                                    case "Y":
                                    case "y":
                                        return deathHandler("Mysterious forses of TES");
                                    default:
                                        inventory[1] = 1;
                                        break;
                                }
                            }
                            if (txt == "") txt = "  Your Health amount was set to " + inventory[0];
                        }
                        catch { txt = "Invalid number."; }
                        break;// Health
                    case "4":
                        try
                        {
                            Console.WriteLine("  1 for addition or subtraction of stats, 2 to set specific value to stat.");
                            val2 = int.Parse(Console.ReadLine()); int val3;
                            Console.WriteLine("  What stat you want to edit?\n   1 for Str, 2 for Agi, 3 for Char, 4 for Endu, 5 for Luck.");
                            int val4 = int.Parse(Console.ReadLine()) + 1;
                            string statTxt = getStatTxt(val4);
                            if (val2 == 1 && val4 >= 2 && val4 <= 6)
                            {
                                Console.WriteLine("  How much you want to add or substract " + statTxt + ". Use negative number for substraction.");
                                val3 = int.Parse(Console.ReadLine());
                                rawChar[val4] += val3;
                            }
                            else if (val2 == 2 && val4 >= 2 && val4 <= 6)
                            {
                                Console.WriteLine("  What value you want your " + statTxt + " to be.");
                                val3 = int.Parse(Console.ReadLine());
                                rawChar[val4] = val3;
                            }
                            else { txt = "Invalid number."; break; }
                            if (txt == "") txt = "  One of your " + statTxt + " was set to " + rawChar[val4];
                        }
                        catch { txt = "Invalid number."; }
                        break;// Stats
                    case "5":
                        if (rawChar[0] == 0) { rawChar[0] = 1; Console.WriteLine("  You changed your gender to Female"); }
                        else if (rawChar[0] == 1) { rawChar[0] = 0; Console.WriteLine("  You changed your gender to Male"); }
                        else { rawChar[0] = 0; Console.WriteLine("  Detected invalid gender. It was set to Male"); }
                        break;// Gender
                    case "6":
                        try
                        {
                            Console.WriteLine("  1 to change time, 2 to change day.");
                            val2 = int.Parse(Console.ReadLine()); int val3;
                            if (val2 == 1)
                            {
                                Console.WriteLine("  What hour you want to set. Value will be crimped between 0 and 23");
                                int val4 = int.Parse(Console.ReadLine()); string time;
                                if (val4 < 0) time = "00";
                                else if (val4 > 23) time = "23";
                                else if (val4 >= 0 && val4 < 10) time = "0" + val4;
                                else time = Convert.ToString(val4);
                                Console.WriteLine("  What minute you want to set. Value will be crimped between 0 and 59");
                                val3 = int.Parse(Console.ReadLine());
                                if (val3 < 0) time += "00";
                                else if (val3 > 59) time += "59";
                                else if (val3 >= 0 && val3 < 10) time += "0" + val3;
                                else time += Convert.ToString(val3);
                                timeLocat[0] = Convert.ToInt32(time);
                                txt = "  Your time was set as " + time;
                            }
                            else if (val2 == 2)
                            {
                                Console.WriteLine("  What day number you want to set.");
                                val3 = int.Parse(Console.ReadLine());
                                if (val3 > 1) val3 = 1;
                                timeLocat[2] = val3;
                                txt = "  Your day number was set to " + timeLocat[2];
                            }
                            else { txt = "Invalid number."; break; }
                        }
                        catch { txt = "Invalid number."; }
                        break;// Time & Day
                    case "7":
                        try
                        {
                            Console.WriteLine("  What location you want to TELEPORT");
                            Console.WriteLine("  FEL FAIN: 5 = start barn, 6 = main street, 7 = inn, 8 = smith, 9 = stable, " +
                                "\n  10 = generalstore, 11 = shrine, 12 = back alley, 13 = castle, 14 = prison");
                            Console.WriteLine("  FEL MARA: 15 = main street, 16 = inn, 17 = smith, 18 = stable, 19 = generalstore," +
                                "\n  20 = alchemist, 21 = temple, 22 = arena, 23 = slavemarket, 24 = monastery," +
                                "\n  25 = back alley, 26 = marketsquare, 27 = castle, 28 = prison");
                            Console.WriteLine("  ROAD: 29 = cave, 30 = crossroads, 31 = inn, 32 = ruins, 33 = mines, 34 = mountains");
                            val2 = int.Parse(Console.ReadLine());
                            if (val2 >= 5 && val2 <= 34) { timeLocat[1] = val2; txt = "  Teleport completed."; }
                            else { txt = "Invalid number."; break; }
                        }
                        catch { txt = "Invalid number."; }
                        break;// tp to Location
                    case "8":
                        try
                        {
                            Console.WriteLine("  What location you want to RESET");
                            Console.WriteLine("  FEL FAIN: 5 = start barn, 6 = main street, 7 = inn, 8 = smith, 9 = stable, " +
                                "\n  10 = generalstore, 11 = shrine, 12 = back alley, 13 = castle, 14 = prison");
                            Console.WriteLine("  FEL MARA: 15 = main street, 16 = inn, 17 = smith, 18 = stable, 19 = generalstore," +
                                "\n  20 = alchemist, 21 = temple, 22 = arena, 23 = slavemarket, 24 = monastery," +
                                "\n  25 = back alley, 26 = marketsquare, 27 = castle, 28 = prison");
                            Console.WriteLine("  ROAD: 29 = cave, 30 = crossroads, 31 = inn, 32 = ruins, 33 = mines, 34 = mountains");
                            val2 = int.Parse(Console.ReadLine());
                            if (val2 >= 5 && val2 <= 34)
                            {
                                string[] lines = File.ReadAllLines("C:\\temp\\txtadventure\\SVfile.txt");
                                lines[val2] = "0";
                                File.WriteAllLines("C:\\temp\\txtadventure\\SVfile.txt", lines);
                                txt = "  Location reset completed.";
                            }
                            else { txt = "Invalid number."; break; }
                        }
                        catch { txt = "Invalid number."; }
                        break;// reset Location
                    case "9":
                        try
                        {
                            Console.WriteLine("  What status you want to change?" +
                                "\n  1 for std, 2 for drunk, 3 for pregnant, 4 for hungry, 5 for hangover," +
                                "\n  6 for skillup (reset), 7 for amount of sex (f) or using threats (m), 8 for mountain info, 9 for horse exhaustion, 10 for hungry baby");
                            string txt2;
                            switch (Console.ReadLine())
                            {
                                case "1":
                                    if (status[1] == 0) txt2 = "Not Active";
                                    else if (status[1] > 0) txt2 = "Hiddenly Activated";
                                    else txt2 = "Active";
                                    Console.WriteLine("  Your Std stage is currently " + txt2 + "\n  What stage you want to set it? 1 for Not Active, 2 for Hiddenly Activated, 3 for Active, 0 to not change it.");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 == 1 || val2 == 2 || val2 == 3)
                                    {
                                        if (status[1] < 0) rawChar[2] -= status[1];
                                        if (val2 == 2) status[1] = timeLocat[2];
                                        else if (val2 == 3) { status[1] = 0 - rawChar[2]; rawChar[2] = 0; }
                                        else status[1] = 0;
                                        if (status[1] == 0) txt2 = "Not Active.";
                                        else if (status[1] > 0) txt2 = "Hiddenly Activated.";
                                        else txt2 = "Active.";
                                        txt = "  Std status changed to " + txt2;
                                    }
                                    else txt = "  Not changing Std status";
                                    break;// std
                                case "2":
                                    Console.WriteLine("  You have currently " + status[4] + " beers active." +
                                        "\n  How many beers you want it to be?");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 < 0) val2 = 0;
                                    if (status[4] == 0 && val2 > 0)
                                    {
                                        if (status[7] == 1) { status[7] = 0; rawChar[2] += 1; rawChar[3] += 1; rawChar[4] += 1; }
                                        status[4] = 1; rawChar[2] += 1; rawChar[3] -= 1; rawChar[4] += 2;
                                    }
                                    else if (status[4] > 0 && val2 == 0) { status[4] = 0; rawChar[2] -= 1; rawChar[3] += 1; rawChar[4] -= 2; }
                                    else status[4] = val2;
                                    txt = "  Your drunk status has been updated as " + status[4] + " beers.";
                                    if (status[4] >= 4) txt += " Next time you'll drink a beer, you will trigger 'Blackout' event.";
                                    break;// drunk
                                case "3":
                                    if (status[5] == -1) Console.WriteLine("  You are currently not pregnant.");
                                    else Console.WriteLine("  You are current pregnancy is at day " + status[5]);
                                    Console.WriteLine("  What pregnancy day would you like to set. -1 for not pregnant, otherwise it will be crimped between -1 and 90" +
                                        "\n  pregnancy levels as days: -1 na, 0-5 hidden, 6-30 minor, 31-60 medium, 61-85 major, 86-90 extreme, 91 birth");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 < -1) val2 = -1;
                                    else if (val2 > 90) val2 = 90;
                                    status[5] = val2;
                                    txt = "  Your pregnancy level was set as " + status[5];
                                    if (status[14] >= 3) { rawChar[3] += 1; rawChar[4] += 1; }
                                    if (status[14] >= 4) rawChar[3] += 1;
                                    if (status[14] >= 5) { rawChar[3] += 4; rawChar[4] += 1; }
                                    status[14] = 1;
                                    if (status[5] >= 6) status[14] = 2;
                                    if (status[5] >= 31) { rawChar[4] -= 1; rawChar[3] -= 1; status[14] = 3; }
                                    if (status[5] >= 61) { rawChar[3] -= 1; status[14] = 4; }
                                    if (status[5] >= 86) { rawChar[3] -= 3; rawChar[4] -= 1; status[14] = 5; }
                                    if (status[5] >= 90) txt += "Next time daily check is initiated (by regular sleep or wait) your baby will be born.";
                                    break;// pregnant
                                case "4":
                                    Console.WriteLine("  Last time you ate was during day " + status[6] + " and current day is " + timeLocat[2] + "\n  What day you want to assing here? Can be future day.");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 < 0) val2 = 0;
                                    status[6] = val2;
                                    txt = "  Your previous meal was set as day " + status[6];
                                    break;// hungry
                                case "5":
                                    if (status[7] == 0) Console.WriteLine("  You currently do not have hangover.");
                                    else Console.WriteLine("  You are currently have hangover.");
                                    Console.WriteLine("  What hangover status you want, 0 for No, 1 for Yes");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (status[7] != val2)
                                    {
                                        if (val2 == 0 || val2 == 1)
                                        {
                                            if (val2 == 0) { status[7] = 0; rawChar[2] += 1; rawChar[3] += 1; rawChar[4] += 1; }
                                            else
                                            {
                                                if (status[4] > 0) { status[7] = 1; status[4] = 0; rawChar[2] -= 2; rawChar[4] -= 3; }
                                                else { status[7] = 1; rawChar[2] -= 1; rawChar[3] -= 1; rawChar[4] -= 1; }
                                            }
                                            txt = "  Your hangover status has been updated.";
                                        }
                                        else txt = "  Your hangover status was not changed.";
                                    }
                                    else txt = "  Your hangover status was not changed.";
                                    break;// hangover
                                case "6":
                                    if (status[9] == 0) txt = "  You had no active skillup potions.";
                                    else { rawChar[status[9]] -= 1; status[8] = 0; status[9] = 0; txt = "  Your skillup potion effect has been reset."; }
                                    break;// skillup reset
                                case "7":
                                    Console.WriteLine("  You'v had sex or used threats " + status[10] + " times.");
                                    Console.WriteLine("  Set number for how many times you have had sex (for female) or used threats (for male)");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 < 0) val2 = 0;
                                    status[10] = val2;
                                    break;// amount of sex or threats
                                case "8":
                                    if (status[11] == 0) Console.WriteLine("  You do not yet have mountain info.\n  Do you want to change it as not found? Y or y to proceed, anything else will not change it.");
                                    else Console.WriteLine("  You have already found mountain info.\n  Do you want to change it as found? Y or y to proceed, anything else will not change it.");
                                    switch (Console.ReadLine())
                                    {
                                        case "Y":
                                        case "y":
                                            if (status[11] == 0) status[11] = 1;
                                            else status[11] = 0;
                                            txt = "  Your knowledge of mountain was changed.";
                                            break;
                                        default:
                                            txt = "  Your knowledge of mountain was not changed.";
                                            break;
                                    }
                                    break;// mountain info
                                case "9":
                                    Console.WriteLine("  Your horse exhaustion is at level " + status[12]);
                                    Console.WriteLine("  What level do you want to set it as? Value will be crimped between 0 and 3.");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 < 0) val2 = 0;
                                    else if (val2 > 3) val2 = 3;
                                    status[12] = val2;
                                    txt = "  Your horses exhaustion level was set as " + status[12];
                                    break;// horse exhaustion
                                case "10":
                                    Console.WriteLine("  Last time you fed your baby/babies was during day " + status[13] + " and current day is " + timeLocat[2] + "\n  What day you want to assing here? Can be future day.");
                                    val2 = int.Parse(Console.ReadLine());
                                    if (val2 < 0) val2 = 0;
                                    status[13] = val2;
                                    txt = "  Last time you fed your baby/babies was set as day " + status[13];
                                    break;// hungry baby
                                default:
                                    txt = "Invalid selection.";
                                    break;
                            }
                        }
                        catch { txt = "Invalid number."; }
                        break;// Statuses
                    case "10":
                        try
                        {
                            Console.WriteLine("  1 for addition or subtraction of Karma 2 to set specific value to Karma.");
                            val2 = int.Parse(Console.ReadLine()); int val3;
                            if (val2 == 1)
                            {
                                Console.WriteLine("  How much Karma you want to add or substract. Use negative number for substraction.");
                                val3 = int.Parse(Console.ReadLine());
                                status[2] += val3;
                            }
                            else if (val2 == 2)
                            {
                                Console.WriteLine("  What value you want your Karma to be.");
                                val3 = int.Parse(Console.ReadLine());
                                status[2] = val3;
                            }
                            else { txt = "Invalid number."; break; }

                            if (txt == "") txt = "  Your Karma was set to " + status[2];
                        }
                        catch { txt = "Invalid number."; }
                        break;// Karma
                    case "11":
                        try
                        {
                            Console.WriteLine("  1 for addition or subtraction of Bounty 2 to set specific value to Bounty.");
                            val2 = int.Parse(Console.ReadLine()); int val3;
                            if (val2 == 1)
                            {
                                Console.WriteLine("  How much Bounty you want to add or substract. Use negative number for substraction.");
                                val3 = int.Parse(Console.ReadLine());
                                status[3] += val3;
                            }
                            else if (val2 == 2)
                            {
                                Console.WriteLine("  What value you want your Bounty to be.");
                                val3 = int.Parse(Console.ReadLine());
                                status[3] = val3;
                            }
                            else { txt = "Invalid number."; break; }

                            if (txt == "") txt = "  Your Bounty was set to " + status[3];
                        }
                        catch { txt = "Invalid number."; }
                        break;// Bounty
                    default:
                        txt = "Invalid number.";
                        break;
                }
                writeSaveFile(rawChar, inventory, timeLocat);
                writeLineToSVFile(status, 3);
                txt += "\n  Press *Enter* to Continue."; Console.WriteLine(txt); Console.ReadLine();
            }
            return true;
        }
        static void openInventory(int[] rawChar, int[] inventory, int[] timeLocat)
        {
            bool cont = true; int val; int val2;
            while (cont)
            {
                bool cont2;
                Console.Clear();
                printHUD(rawChar, inventory, timeLocat);
                Console.WriteLine("  1 to print item info, 2 to use item, 3 to move item to other slot, 0 to close inventory");
                switch (Console.ReadLine())
                {
                    case "0":
                        cont = false;
                        break;// close
                    case "1":
                        cont2 = true;
                        while (cont2)
                        {
                            Console.Clear();
                            printHUD(rawChar, inventory, timeLocat);
                            Console.WriteLine("  Select inventory slot for item info, or 10 for Horse Exhaustion, Current Bounty and Current Karma");
                            try
                            {
                                val = int.Parse(Console.ReadLine()); string txt = "";
                                int itemId = inventory[val + 3];
                                if (val >= 1 && val <= 9 && itemId != 0)
                                {
                                    int[] itemStats = readLineFromItems(itemId);
                                    if (itemStats[1] == 1) txt += "\n  This item is discardable.";
                                    else txt += "\n  This item is not discardable.";
                                    if (itemStats[2] == 1) txt += "\n  This item is considered as illeagal.";
                                    else txt += "\n  This item is leagal.";
                                    if (itemStats[3] == 1) txt += "\n  This item has plausible activation.";
                                    else txt += "\n  This item does not have activation.";
                                    if (itemId == 5) txt += "\n  Last time you have fed your baby/ babies is on Day: " + readSlotFromSVFile(3, 13);// baby
                                    else if (itemId == 6) txt += "\n  Last time you have eaten is on Day: " + readSlotFromSVFile(3, 6);// ration
                                    Console.WriteLine("  This slot contains item '{0}'." +
                                        "\n  It's estimated value is {1} Gold.{2}\n\n  Press *Enter* to proceed."
                                        , getItemTxtWithJustId(itemId), itemStats[0], txt);
                                    Console.ReadLine(); cont2 = false;
                                }
                                else if (val == 10)
                                {
                                    int[] status = readLineFromSVFileForEvent(3);
                                    Console.WriteLine("  Your horses exhaustion level is: " + status[12]);
                                    Console.WriteLine("  Your Current Bounty is: " + status[3]);
                                    Console.WriteLine("  Your Current Bounty is: " + status[2]);
                                    Console.WriteLine("\n  Press *Enter* to proceed."); Console.ReadLine(); cont2 = false;
                                }
                                else if (inventory[val + 3] == 0)
                                { Console.WriteLine("  This slot is empty, press *Enter* to proceed."); Console.ReadLine(); cont2 = false; }
                                else
                                { Console.WriteLine("Invalid number, press *Enter* and try again."); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid selection, press *Enter* and try again."); Console.ReadLine(); }
                        }
                        break;// info
                    case "2":
                        cont2 = true;
                        while (cont2)
                        {
                            Console.Clear();
                            printHUD(rawChar, inventory, timeLocat); int[] status = readLineFromSVFileForEvent(3);
                            Console.WriteLine("  Select inventory slot for item you want to activate or use.");
                            try
                            {
                                val = int.Parse(Console.ReadLine()); int itemId = inventory[val + 3];
                                if (val >= 1 && val <= 9 && itemId != 0)
                                {
                                    int[] itemStats = readLineFromItems(itemId); int usable = itemStats[3]; string infoTxt; Random rnd = new Random();
                                    if (usable == 1)
                                    {
                                        switch (itemId)
                                        {
                                            case 5:
                                                infoTxt = "  Feeding '" + getItemTxtWithJustId(itemId) + "' will consume ration from your inventory. One ration is enough to feed all babies you have.";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    bool rationIs = false; int i;
                                                    for (i = 4; i > 12; i++)
                                                    {
                                                        if (inventory[i] == 6) { rationIs = true; break; }
                                                    }
                                                    if (rationIs == false) { Console.WriteLine("  You can't feed your baby since you don't have any rations."); }
                                                    else { Console.WriteLine("  You have fed your babies one of your rations."); inventory[i] = 0; }
                                                    //inventory[] = 0;
                                                }
                                                break;// baby
                                            case 6:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. This will satiate your hunger.";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0; status[6] = timeLocat[2];
                                                    Console.WriteLine("  You feel full after eating ration.");
                                                }
                                                break;// ration
                                            case 7:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. Drinking this will get you drunk that will temporarily boost your Str and Char, while lowering agility." +
                                                    "\n  Hangover will temporarily lower all 3 of those stats. Drinking while in hangover will remove hangover debuff and apply new drunk buff." +
                                                    "\n  Just be careful and drink in moderation, you wouldn't want to teleport and 'missplace' some of your items and/or gold, would you?";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0; int drunk = status[4]; int hangover = status[7];
                                                    if (hangover == 1) { status[7] = 0; rawChar[2] += 1; rawChar[3] += 1; rawChar[4] += 1; Console.WriteLine("  You forget your hangover quite quickly after another brew."); }
                                                    if (drunk == 0) { status[4] = 1; rawChar[2] += 1; rawChar[3] -= 1; rawChar[4] += 2; Console.WriteLine("  You feel drunk, strong and charismatic."); }
                                                    else if (drunk > 0 && drunk <= 5) { status[4] = drunk++; Console.WriteLine(" You drank another beer but it feels like it didn't do anything."); }
                                                    else if (drunk > 5)
                                                    {
                                                        int rahaKadotus = rnd.Next(0, 250 - rawChar[6] * 30);
                                                        int itemKadotusTod = rnd.Next(0, 125 - rawChar[6] * 10);
                                                        int itemKadotus = rnd.Next(3, 12);
                                                        if (rahaKadotus < inventory[0] && rahaKadotus > 0) inventory[0] -= rahaKadotus;
                                                        else if (rahaKadotus > inventory[0]) inventory[0] = 0;
                                                        if (itemKadotusTod >= 50) inventory[itemKadotus] = 0;
                                                        if (timeLocat[1] >= 5 && timeLocat[1] <= 14)// Fel Fain
                                                        {
                                                            int teleport = rnd.Next(0, 12);
                                                            if (teleport == 8) teleport = 13;
                                                            else if (teleport == 10) teleport = 14;
                                                            timeLocat[1] = teleport;
                                                        }
                                                        else if (timeLocat[1] >= 15 && timeLocat[1] <= 28)// Fel Mara
                                                        {
                                                            int teleport = rnd.Next(15, 25);
                                                            if (teleport == 17) teleport = 26;
                                                            else if (teleport == 19) teleport = 27;
                                                            else if (teleport == 20) teleport = 28;
                                                            timeLocat[1] = teleport;
                                                        }
                                                        else
                                                        {
                                                            int teleport = rnd.Next(29, 33);
                                                            timeLocat[1] = teleport;
                                                        }
                                                        updateTimeAndReturnTxt(timeLocat, 1080, rawChar, inventory);
                                                        Console.WriteLine("  Your memory goes blank and you wake up from a random place. You have no recollection of what you might have done.");
                                                    }
                                                }
                                                break;// beer
                                            case 8:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "' txt";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0;
                                                }
                                                break;// wild potion
                                            case 9:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. Drinking this will cure any illnesses from hangover to std's," +
                                                    "\n  and the best part is that for this effect to apply, you don't need gods approval.";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0; int std = status[1]; int hangover = status[7];
                                                    if (hangover == 1) { status[7] = 0; rawChar[2] += 1; rawChar[3] += 1; rawChar[4] += 1; }
                                                    if (std == 1) status[1] = 0;
                                                    else if (std < 0) { rawChar[5] -= std; status[1] = 0; }
                                                    Console.WriteLine("  Whatever physical was ailing you is now gone. Your mental stage will remain the same.");
                                                }
                                                break;// cure potion
                                            case 12:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. This will reduce horses exhaustion back to 12 points.";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0; status[6] = 0; Console.WriteLine("  Your horse seems satisfied and revitalized.");
                                                }
                                                break;// hay
                                            case 14:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. Will heal you by 1 point or up to max Health.";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0;
                                                    if (inventory[1] < rawChar[5] * 2)
                                                    {
                                                        inventory[1] += 1;
                                                        if (inventory[1] > rawChar[5] * 2) inventory[1] = rawChar[5] * 2;
                                                        Console.WriteLine("  You feel a bit better now since the minor scracthes are gone.");
                                                    }
                                                    Console.WriteLine("  You feel a bit better now, even though it might just be plasebo.");
                                                }
                                                break;// hp potion minor
                                            case 15:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. Will heal you by 4 points or up to max Health.";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0;
                                                    if (inventory[1] < rawChar[5] * 2)
                                                    {
                                                        inventory[1] += 4;
                                                        if (inventory[1] > rawChar[5] * 2) inventory[1] = rawChar[5] * 2;
                                                        Console.WriteLine("  You feel a bit better now since some bigger wounds are healed up.");
                                                    }
                                                    Console.WriteLine("  You feel a bit better now, even though it might just be plasebo.");
                                                }
                                                break;// hp potion major
                                            case 16:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. Drinking this concoction will give you temporary boost on one of your base stats." +
                                                    "\n  But only one buff from this kind of potion can be active at a time. If you already have one such buff, this will replace it." +
                                                    "\n\n  Input skill to improve: // 1 for Str // 2 for Agi // 3 for Char // 4 for Endu // 5 for Luck //";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0;
                                                    if (status[8] != 0) rawChar[status[9]] -= 1;
                                                    status[8] = timeLocat[2];
                                                    while (cont2)
                                                    {
                                                        Console.Clear(); printHUD(rawChar, inventory, timeLocat); Console.WriteLine(infoTxt);
                                                        switch (Console.ReadLine())
                                                        {
                                                            case "1":
                                                                rawChar[2] += 1; status[9] = 2; cont2 = false;
                                                                break;
                                                            case "2":
                                                                rawChar[3] += 1; status[9] = 3; cont2 = false;
                                                                break;
                                                            case "3":
                                                                rawChar[4] += 1; status[9] = 4; cont2 = false;
                                                                break;
                                                            case "4":
                                                                rawChar[5] += 1; status[9] = 5; cont2 = false;
                                                                break;
                                                            case "5":
                                                                rawChar[6] += 1; status[9] = 6; cont2 = false;
                                                                break;
                                                            default:
                                                                Console.WriteLine("  This item doesn't have activation, press *Enter* to continue."); Console.ReadLine();
                                                                break;
                                                        }
                                                        if (cont2 == false) Console.WriteLine("  You feel invigorated.");
                                                    }
                                                }
                                                break;// skillup potion timed
                                            case 17:
                                                infoTxt = "  Using item '" + getItemTxtWithJustId(itemId) + "'. Drinking this concoction will give you permanently boost on one of your base stats." +
                                                    "\n  Though it's name would implie that it contains Voxor Ore. Who even though putting mysterious rocks into a potion would be a great idea?" +
                                                    "\n\n  Input skill to improve: // 1 for Str // 2 for Agi // 3 for Char // 4 for Endu // 5 for Luck //";
                                                if (useItemConfirm(infoTxt) == true)
                                                {
                                                    inventory[itemId] = 0;
                                                    while (cont2)
                                                    {
                                                        Console.Clear(); printHUD(rawChar, inventory, timeLocat); Console.WriteLine(infoTxt);
                                                        switch (Console.ReadLine())
                                                        {
                                                            case "1":
                                                                rawChar[2] += 1; cont2 = false;
                                                                break;
                                                            case "2":
                                                                rawChar[3] += 1; cont2 = false;
                                                                break;
                                                            case "3":
                                                                rawChar[4] += 1; cont2 = false;
                                                                break;
                                                            case "4":
                                                                rawChar[5] += 1; cont2 = false;
                                                                break;
                                                            case "5":
                                                                rawChar[6] += 1; cont2 = false;
                                                                break;
                                                            default:
                                                                Console.WriteLine("  This item doesn't have activation, press *Enter* to continue."); Console.ReadLine();
                                                                break;
                                                        }
                                                        if (cont2 == false) Console.WriteLine("  You feel invigorated.");
                                                    }
                                                }
                                                break;// evolution potion perm                                                                                                
                                        }
                                        writeLineToSVFile(status, 3);
                                        Console.WriteLine("  Press *Enter* to continue."); Console.ReadLine(); cont2 = false;
                                    }
                                    else { Console.WriteLine("  This item doesn't have activation, press *Enter* to continue."); Console.ReadLine(); cont2 = false; }
                                }
                                else if (itemId == 0)
                                { Console.WriteLine("  This slot is empty, press *Enter* to continue."); Console.ReadLine(); cont2 = false; }
                                else
                                { Console.WriteLine("Invalid number, press *Enter* and try again."); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid selection, press *Enter* and try again."); Console.ReadLine(); }
                        }
                        break;// use item
                    case "3":
                        cont2 = true;
                        while (cont2)
                        {
                            Console.Clear();
                            printHUD(rawChar, inventory, timeLocat);
                            Console.WriteLine("  Select inventory slot from where you want to move item.");
                            try
                            {
                                val = int.Parse(Console.ReadLine()); int itemId = inventory[val + 3];
                                if (val >= 1 && val <= 9 && itemId != 0)
                                {
                                    while (cont2)
                                    {
                                        Console.Clear();
                                        printHUD(rawChar, inventory, timeLocat);
                                        Console.WriteLine("  Select inventory slot to where you want to move item. if ");
                                        try
                                        {
                                            val2 = int.Parse(Console.ReadLine()); int itemId2 = inventory[val2 + 3];
                                            if (val2 >= 1 && val2 <= 9)
                                            { inventory[val2 + 3] = itemId; inventory[val + 3] = itemId2; cont2 = false; }
                                            else
                                            { Console.WriteLine("Invalid number, press *Enter* and try again."); Console.ReadLine(); }
                                        }
                                        catch { Console.WriteLine("Invalid selection, press *Enter* and try again."); Console.ReadLine(); }
                                    }
                                }
                                else if (itemId == 0)
                                { Console.WriteLine("  This slot is empty, press *Enter* to continue."); Console.ReadLine(); cont2 = false; }
                                else
                                { Console.WriteLine("Invalid number, press *Enter* and try again."); Console.ReadLine(); }
                            }
                            catch { Console.WriteLine("Invalid selection, press *Enter* and try again."); Console.ReadLine(); }
                        }
                        break;// swap slots
                    default:
                        Console.WriteLine("Invalid selection, press *Enter* and try again.");
                        Console.ReadLine();
                        break;
                }
                writeSaveFile(rawChar, inventory, timeLocat);
                Console.Clear();
            }
        }
        static int[] createChar(int[] rawChar)
        {
            //int[] rawChar = new int[7]; // 0 = Gender {0 male, 1 female}, 1 = Class {0 warrior, 1 rogue, 2 merchant}, 2 = #Str, 3 = #Agi, 4 = #Char, 5 = #Endu, 6 = #Luck
            bool cont;
            bool retry = true;
            while (retry)
            {
                cont = true;
                while (cont)
                {
                    createCharTxt();
                    Console.WriteLine("\n  Choose Gender:\n  M or m for Male\n  F or f for Female\n  I or i for skill & attribute Info");
                    switch (Console.ReadLine())
                    {
                        case "I":
                        case "i":
                        case "Info":
                        case "info":
                            createCharInfo();
                            Console.Clear();
                            break;
                        case "M":
                        case "m":
                        case "Male":
                        case "male":
                        case "1":
                            rawChar[0] = 0; rawChar[2] += 1; rawChar[5] += 1; rawChar[3] -= 1;
                            cont = false;
                            Console.Clear();
                            break;
                        case "F":
                        case "f":
                        case "Female":
                        case "female":
                        case "2":
                            rawChar[0] = 1; rawChar[3] += 1; rawChar[4] += 1; rawChar[2] -= 1;
                            cont = false;
                            Console.Clear();
                            break;
                        default:
                            Console.WriteLine("Unknown gender selection, press *Enter* and try again");
                            Console.ReadLine();
                            Console.Clear();
                            break;
                    }
                }
                cont = true;
                while (cont)
                {
                    createCharTxt();
                    Console.WriteLine("\n  Choose Class:\n  A or a for Warrior\n  B or b for Rogue\n  C or c for Merchant\n  I or i for skill & attribute Info");
                    switch (Console.ReadLine())
                    {
                        case "I":
                        case "i":
                        case "Info":
                        case "info":
                            createCharInfo();
                            Console.Clear();
                            break;
                        case "A":
                        case "a":
                        case "W":
                        case "w":
                        case "Warrior":
                        case "warrior":
                        case "1":
                            rawChar[1] = 0; rawChar[2] += 4; rawChar[3] += 1; rawChar[4] += 2; rawChar[5] += 3; rawChar[6] += 2;
                            cont = false;
                            Console.Clear();
                            break;
                        case "B":
                        case "b":
                        case "R":
                        case "r":
                        case "Rogue":
                        case "rogue":
                        case "2":
                            rawChar[1] = 1; rawChar[2] += 2; rawChar[3] += 4; rawChar[4] += 2; rawChar[5] += 1; rawChar[6] += 3;
                            cont = false;
                            Console.Clear();
                            break;
                        case "C":
                        case "c":
                        case "M":
                        case "m":
                        case "Merchant":
                        case "merchant":
                        case "3":
                            rawChar[1] = 2; rawChar[2] += 1; rawChar[3] += 2; rawChar[4] += 4; rawChar[5] += 2; rawChar[6] += 3;
                            cont = false;
                            Console.Clear();
                            break;
                        default:
                            Console.WriteLine("Unknown Class selection, press *Enter* and try again");
                            Console.ReadLine();
                            Console.Clear();
                            break;
                    }
                }
                cont = true;
                while (cont)
                {
                    printChar(rawChar);
                    Console.WriteLine("\n  Do you want this to be your character?\n  If Yes, type Y or y\n  If you want to start over and create another, then type N or n");
                    switch (Console.ReadLine())
                    {
                        case "Y":
                        case "y":
                        case "Yes":
                        case "yes":
                        case "1":
                            Console.Clear();
                            retry = false; cont = false;
                            break;
                        case "N":
                        case "n":
                        case "No":
                        case "no":
                        case "2":
                        case "new":
                        case "New":
                        case "Retry":
                        case "retry":
                            Console.Clear();
                            for (int i = 0; i < rawChar.Length; i++)
                                rawChar[i] = 0;
                            cont = false;
                            break;
                        default:
                            Console.WriteLine("Unknown selection, press *Enter* and try again");
                            Console.ReadLine();
                            Console.Clear();
                            break;
                    }
                }
            }
            return rawChar;
        }
        static int[] startInventory(int[] rawChar, int[] inventory)
        {
            const int hpmulti = 2;
            //const int invKoko = 13; // Huom; Raha, HP, hevonen ja ase vie dedicoidut 4 ekaa slottia
            //int[] inventory = new int[invKoko];
            inventory[1] = rawChar[5] * hpmulti;
            if (rawChar[1] == 0)
            {
                inventory[0] = 25;
                inventory[3] = 1;
            }
            else if (rawChar[1] == 1)
            {
                inventory[0] = 5;
                inventory[3] = 2;
            }
            else
            {
                inventory[0] = 100;
                inventory[3] = 3;
            }
            return inventory;
        }
        static string updateTimeAndReturnTxt(int[] timeLocat, int step, int[] rawChar, int[] inventory) // example: time 1630, step 90
        {
            string txt;
            int tunnit = timeLocat[0] / 100;
            int minuutit = timeLocat[0] % 100;
            minuutit += step;
            if (minuutit >= 60)
            {
                tunnit += minuutit / 60;
                minuutit = minuutit % 60;
                bool cont = true;
                while (cont)
                if (tunnit >= 24)
                    { 
                        tunnit -= 24; timeLocat[2] += 1;
                        dailyChecks(rawChar, inventory, timeLocat);
                    }
                else
                    cont = false;
            }
            timeLocat[0] = tunnit * 100 + minuutit;
            if (tunnit >= 10)
                txt = Convert.ToString(tunnit);
            else
                txt = "0" + tunnit;
            if (minuutit >= 10)
                txt += ":" + minuutit;
            else
                txt += ":0" + minuutit;
            return txt;
        }
        static int valintaRak(int[] timeLocat, int[] rawChar, int[] inventory)
        {
            int location = timeLocat[1];
            int igTime = timeLocat[0];
            int[] mahdValinnat = locationChoices(location, rawChar, igTime); // {0, 1,..., n}
            bool cont = true;
            int val = 0;
            while (cont)
            {
                string valinta = Console.ReadLine();
                switch (valinta)
                {
                    case "Q":
                    case "q":
                        Console.WriteLine("Do you want to Quit?\nThis game will save progress\nSavefile location is C:\\temp\\txtadventure\\SVfile.txt \n\nEnter Y or y to proceed or Make new choice");
                        switch (valinta = Console.ReadLine())
                        {
                            case "Y":
                            case "y":
                                return -1;
                        }
                        break;
                    case "I":
                    case "i":
                        return -2;
                    case "tes":
                        return -3;

                }// Quit ja Inventory tarkistus                
                try
                {
                    val = int.Parse(valinta);
                    if (val >= 0 && val <= mahdValinnat[mahdValinnat.Length - 1])
                        cont = false;
                    else
                    {
                        Console.WriteLine("Invalid number, press *Enter and try again...");
                        Console.ReadLine();
                        Console.Clear();
                        printHUD(rawChar, inventory, timeLocat);
                        locationDescriptions(location);
                        locationChoicesTxtAndAmount(location, rawChar, igTime);
                    }
                }
                catch
                {
                    Console.WriteLine("Invalid selection, press *Enter and try again...");
                    Console.ReadLine();
                    Console.Clear();
                    printHUD(rawChar, inventory, timeLocat);
                    locationDescriptions(location);
                    locationChoicesTxtAndAmount(location, rawChar, igTime);
                }// Valinnan validointi -> int val
            }
            return val;
        }
        // Basic Sub Funcs
        static bool useItemConfirm(string infoTxt)
        {
            bool useit = false; Console.WriteLine(infoTxt); Console.WriteLine("  If you want to use or activate this, input Y or y, any other input will cancel this action.");
            switch (Console.ReadLine())
            {
                case "Y":
                case "y":
                    useit = true;
                    break;
                default:
                    Console.WriteLine("  Cancelled item usage or activation. Returning to inventory");
                    break;
            }
            return useit;
        }
        static bool deathHandler(string causeOfDeathTxt)
        {
            File.Delete("C:\\temp\\txtadventure\\SVfile.txt");
            Console.WriteLine("  You died, cause of death was " + causeOfDeathTxt + "\n\n  Closing the game and deleting savefile. Press *Enter*.");
            Console.ReadLine();
            return true;
        }
        // Event Handler Subs
        static int[] locationChoices(int location, int[] rawChar, int igTime)
        {
            int määrä = locationChoicesTxtAndAmount(location, rawChar, igTime);
            int[] choices = new int[määrä];
            for (int i = 0; i < choices.Length; i++)
                choices[i] = i;
            return choices;
        }
        static void eventPrintTxt(string txt)
        {
            Console.WriteLine(txt);
            Console.WriteLine("\n  Press *Enter* to proceed");
            Console.ReadLine();
        }
        static void passTime(int[] timeLocat, bool TrueSleep_FalseWait, int[] inventory, int[] rawChar)
        {
            int step = 0; bool cont = true;
            while (cont)
            {
                if (TrueSleep_FalseWait == true)
                    Console.WriteLine("  How many hours do you want to sleep? Give value between 3 and 24.\n  Each full 3h will heal you by 1 point and decrease horse exhaustion by 1 point.");
                else
                    Console.WriteLine("  How many minutes do you want to sleep? Give value between 1 and 60.");
                try
                {
                    step = int.Parse(Console.ReadLine());
                    if (TrueSleep_FalseWait == true)
                    {
                        if (step >= 3 && step <= 24)
                        {
                            int maxHp = readSlotFromSVFile(0, 5) * 2;
                            int horseExhaust = readSlotFromSVFile(3, 12);
                            bool t1 = true; bool t2 = true;
                            for (int temp = step / 3; temp > 0; temp--)
                            {
                                if (maxHp > inventory[1])
                                    inventory[1] += 1;
                                else
                                    t1 = false;
                                if (horseExhaust > 0)
                                    horseExhaust -= 1;
                                else
                                    t2 = false;
                                if (t1 == false && t2 == false)
                                    break;
                            }
                            writeSlotToSVFile(3, 12, horseExhaust);
                            cont = false; step *= 60;
                        }
                        else
                            Console.WriteLine("Invalid value, try again.");
                    }
                    else
                    {
                        if (step >= 1 && step <= 60)
                            cont = false;
                        else
                            Console.WriteLine("Invalid value, try again.");
                    }
                }
                catch
                {
                    Console.WriteLine("Invalid input, try again.");
                }
            }
            updateTimeAndReturnTxt(timeLocat, step, rawChar, inventory);
        }
        static bool buyItemSub(int[] inventory, int id, int invSlot, int itemId)
        {
            bool cont = true;
            if (id != 0)
            {
                int[] itemStat = readLineFromItems(id);
                if (itemStat[1] == 1)
                {
                    Console.WriteLine("  WARNING! Do you want to discard item '{0}'?\n  Y or y for confirmation, empty for cancellation.", getItemTxtWithJustId(id));
                    switch (Console.ReadLine())
                    {
                        case "Y":
                        case "y":
                            inventory[invSlot] = itemId; cont = false;
                            break;
                        default:
                            Console.WriteLine("  Cancelled discarding item, pick another slot.");
                            break;
                    }
                }
                else { Console.WriteLine("  This item cannot be discarded. Please select another slot."); }
            }
            else { inventory[invSlot] = itemId; cont = false; }
            return cont;
        }
        static bool buyItem(int[] inventory, int price, int itemId)
        {
            bool success = false; bool room = false;
            for (int i = 0; i < 9; i++)
            {
                int item = inventory[i + 4];
                if (item != 0)
                {
                    int[] itemLine = readLineFromItems(item);
                    if (itemLine[1] == 1)
                    { room = true; break; }
                }
                else { room = true; break; }
            }
            if (inventory[0] >= price && room == true)
            {
                inventory[0] -= price; bool cont = true;
                Console.WriteLine("  To wich inventory slot you want to put your new item? Input number shown on inventory slot's upper corner." +
                    "\n  If slot is already occupied, your old item will be discarded.");
                while (cont)
                {
                    try
                    {
                        int invSlot = int.Parse(Console.ReadLine());
                        if (invSlot >= 1 && invSlot <= 9)
                        {
                            invSlot += 3; int id = inventory[invSlot];
                            cont = buyItemSub(inventory, id, invSlot, itemId);
                        }
                        else Console.WriteLine("  Invalid number. Please try again.");
                    }
                    catch { Console.WriteLine("  Invalid selection. Please try again."); }
                }
                success = true;
            }
            else if (room == false) { Console.WriteLine("  Full on non-discardable items. Press *Enter* to continue."); Console.ReadLine(); }
            else { Console.WriteLine("  You don't have enough Gold to buy this. Press *Enter* to continue."); Console.ReadLine(); }
            return success;
        }
        // Event Handler; Edit Location Subs
        static void locationDescriptions(int location)
        {
            int[] info; int arrayPituus = 5; // if change, edit also readLineFromSVFileForEvent()
            switch (location)
            {                
                case 5:
                    if (false == checkIfLineExistsInSVFile(location))
                    { int[] empty = new int[arrayPituus]; writeLineToSVFile(empty, location); info = empty; }
                    else
                        info = readLineFromSVFileForEvent(location);
                    if (info[0] == 0)
                    {
                        info[0] = 1; writeLineToSVFile(info, location);
                        Console.WriteLine("  You wake up from barn. It seems like after spending evening in local inn you somehow ended up sleeping in a barn.\n  You gather your gear and prepare to continue your search of the rumored treasure.\n  This area doesn't have much to do, but you could spend some time to see if there is some loot."); 
                    }
                    else
                        Console.WriteLine("  So your back in here. But you feel like you don't want to sleep another night here. Maybe you missed some loot?");
                    break;// barn (Fain)
                case 6:
                    if (false == checkIfLineExistsInSVFile(location))
                    { int[] empty = new int[arrayPituus]; writeLineToSVFile(empty, location); info = empty; }
                    else
                        info = readLineFromSVFileForEvent(location);
                    if (info[0] == 0)
                    {
                        info[0] = 1; writeLineToSVFile(info, location);
                        Console.WriteLine("  As you step outside from the barn, the bright sun greets you. Fel Fains main street seems to be quite busy.");
                    }
                    else
                        Console.WriteLine("  You are back in the main street of Fel Fain. The gateway to almost everywhere in this city.");
                    break;// main street (Fain)
                case 7:
                    if (false == checkIfLineExistsInSVFile(location))
                    { int[] empty = new int[arrayPituus]; writeLineToSVFile(empty, location); info = empty; }
                    else
                        info = readLineFromSVFileForEvent(location);
                    Console.WriteLine("  This inn isn't that crowded, but still, it's not empty.\n  Perfect place to try to find some rumors, spend some time or steal from the patrons.");
                    break;// inn (Fain)
            }
        }// Paikkojen kuvaukset
        static int locationChoicesTxtAndAmount(int location, int[] rawChar, int igTime)
        {
            // HUOMIOI jotkut skillit tai muut asiat voi antaa lisä valintoja
            int määrä = 0;
            Console.WriteLine();
            switch (location)
            {
                case 5:
                    Console.Write("  0 to go to outside, 1 to try to find loot,");
                    määrä = 2;
                    break;// barn (Fain)
                case 6:
                    Console.Write("  0 to start travelling to city of Fel Mara, 1 go to barn, 2 go to inn, 3 go to smith, 4 go to stable, 5 go to general store," +
                        "\n  6 go to shrine, 7 go to back alleys, 8 go to castle, 9 to search for loot,");
                    määrä = 10;
                    break;// main Street (Fain)
                case 7:
                    Console.Write("  0 to go back to Main Street, 1 to buy supplies, 2 to rent bed (sleep), 3 to wait, 4 ask for rumors, 5 for gambling your gold," +
                        "\n  6 to try to steal something,");
                    määrä = 8;
                    if (readSlotFromSVFile(0, 0) == 1) Console.Write(" 7 to try to make profit by seduction,");
                    else Console.Write(" 7 to start a brawl by threatening to get some loot,");
                    break;// inn (Fain)
            }
            Console.Write(" For Inventory use I or i, For Save & Quit use Q or q");
            Console.WriteLine();
            return määrä;
        }// Valintojen kuvaukset Ja määrä
        // Event Handler
        static bool eventHandler(int[] rawChar, int[] inventory, int[] timeLocat , int valinta)
        {
            string txt; int[] info; Random rnd = new Random(); bool deathQuit = false;
            info = readLineFromSVFileForEvent(timeLocat[1]);
            int[] status = readLineFromSVFileForEvent(3);
            int pregmod = readSlotFromSVFile(3, 14);
            switch (timeLocat[1]) // Sijainti
            {                
                case 5:                     
                    switch (valinta)
                    {
                        case 0:
                            timeLocat[1] = 6;
                            txt = "  You step outside of the barn to the Main Street of Fel Fain.";
                            eventPrintTxt(txt);
                            break;// ->street
                        case 1:
                            if (info[1] == 0 || (timeLocat[2]-info[1] >= 3))
                            {
                                info[1] = timeLocat[2]; writeLineToSVFile(info, timeLocat[1]);
                                int spentTime = (rnd.Next(50 - rawChar[6], 70 - rawChar[6])) * pregmod; updateTimeAndReturnTxt(timeLocat, spentTime, rawChar, inventory);
                                int gold = rnd.Next((rawChar[6] + rawChar[3]) / 2 , rawChar[6] + rawChar[3]); inventory[0] += gold;
                                txt = "  You spent " + spentTime + " minutes and managed to find " + gold + " Gold.";
                            }
                            else
                                txt = "  You just recently looted this place, maybe in a few days there would be more loot.";
                            eventPrintTxt(txt);
                            break;// looting
                    }
                    break;// Barn (Fain) {ekaVisit, dayLooted}
                case 6:
                    switch (valinta)
                    {
                        case 0:
                            txt = "  TESTI TXT -> travel to Mara";
                            eventPrintTxt(txt);
                            break;
                        case 1:
                            timeLocat[1] = 5;
                            txt = "  You decided to head back to familiar barn.";
                            eventPrintTxt(txt);
                            break;// ->barn
                        case 2:
                            timeLocat[1] = 7;
                            txt = "  'Halfmoon Inn' looks cosy and you decide to step inside.";
                            eventPrintTxt(txt);
                            break;// ->'Halfmoon Inn'
                        case 3:
                            txt = "  TESTI TXT -> go to smith";
                            eventPrintTxt(txt);
                            break;
                        case 4:
                            txt = "  TESTI TXT -> go to stable";
                            eventPrintTxt(txt);
                            break;
                        case 5:
                            txt = "  TESTI TXT -> go to general store";
                            eventPrintTxt(txt);
                            break;
                        case 6:
                            txt = "  TESTI TXT -> go to shrine";
                            eventPrintTxt(txt);
                            break;
                        case 7:
                            txt = "  TESTI TXT -> go to back alleys";
                            eventPrintTxt(txt);
                            break;
                        case 8:
                            txt = "  TESTI TXT -> go to castle";
                            eventPrintTxt(txt);
                            break;
                        case 9:
                            if (info[1] == 0 || (timeLocat[2] - info[1] >= 1))
                            {
                                info[1] = timeLocat[2]; writeLineToSVFile(info, timeLocat[1]);                                
                                int spentTime = (rnd.Next(60 - rawChar[6] * 2, 120 - rawChar[6] * 2)) * pregmod; updateTimeAndReturnTxt(timeLocat, spentTime, rawChar, inventory);
                                int gold = rnd.Next(rawChar[6] + rawChar[3], 10 + 2 * (rawChar[6] + rawChar[3])); inventory[0] += gold;
                                txt = "  You spent " + spentTime + " minutes and managed to find " + gold + " Gold.";
                            }
                            else
                                txt = "  You just recently looted this place, maybe tomorrow there would be more loot.";
                            eventPrintTxt(txt);
                            break;// looting
                    }
                    break;// Main Street (Fain) {ekaVisit, dayLooted}
                case 7:
                    switch (valinta)
                    {
                        case 0:
                            timeLocat[1] = 6;
                            txt = "  You decide to head back to Main Street.";
                            eventPrintTxt(txt);
                            break;// ->street
                        case 1:
                            bool cont1 = true;
                            while (cont1)
                            {
                                Console.Clear(); printHUD(rawChar, inventory, timeLocat);
                                Console.WriteLine("  This inn provides some nourishments if you have enough Gold." +
                                    "\n\n  The following items can be bough from here:\n");
                                // ration // items[0] = 2; items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 6);
                                int[] ratioLine = readLineFromItems(6); int ratioPrice;
                                if (rawChar[4] >= 5) ratioPrice = ratioLine[0] - 1;
                                else if (rawChar[4] <= 2) ratioPrice = ratioLine[0] + 1;
                                else ratioPrice = ratioLine[0];
                                Console.WriteLine("  # 1 // Ration, {0} Gold. This item is good when you or someone else is hungry.", ratioPrice);
                                // beer // items[0] = 5; items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 7);
                                int[] beerLine = readLineFromItems(7); int beerPrice;
                                if (rawChar[4] >= 5) beerPrice = beerLine[0] - 1;
                                else if (rawChar[4] == 2) beerPrice = beerLine[0] + 1;
                                else if (rawChar[4] <= 1) beerPrice = beerLine[0] + 2;
                                else beerPrice = beerLine[0];
                                Console.WriteLine("  # 2 // Beer, {0} Gold. This item gets you drunk today, and hangover tommorrow. Gives some buffs and debuffs.", beerPrice);
                                // hay // items[0] = 10; items[1] = 1; items[2] = 0; items[3] = 1; writeLineToItems(items, 12);
                                int[] hayLine = readLineFromItems(12); int hayPrice;
                                if (rawChar[4] >= 5) hayPrice = hayLine[0] - 3;
                                else if (rawChar[4] == 4) hayPrice = hayLine[0] - 1;
                                else if (rawChar[4] == 2) hayPrice = hayLine[0] + 2;
                                else if (rawChar[4] == 1) hayPrice = hayLine[0] + 5;
                                else if (rawChar[4] <= 0) hayPrice = hayLine[0] + 10;
                                else hayPrice = hayLine[0];
                                Console.WriteLine("  # 3 // Haybale, {0} Gold. Give some to your exhausted horse so it can run a bit longer.", hayPrice);
                                Console.WriteLine("\n  Input item number to buy it, or input 0 to stop shopping.");
                                switch (Console.ReadLine())
                                {
                                    case "0":
                                        cont1 = false;
                                        break;
                                    case "1":
                                        buyItem(inventory, ratioPrice, 6);                                        
                                        break;
                                    case "2":
                                        buyItem(inventory, beerPrice, 7);
                                        break;
                                    case "3":
                                        buyItem(inventory, hayPrice, 12);
                                        break;
                                    default:
                                        Console.WriteLine("Invalid input. Press *Enter* to try again."); Console.ReadLine(); break;
                                }
                                writeSaveFile(rawChar, inventory, timeLocat);
                            }
                            txt = "  You'll step away from the bar counter.";
                            eventPrintTxt(txt);
                            break;// shop
                        case 2:
                            Console.WriteLine("  Bed rental costs 3 Gold. Do you want to rent it? Y or y for confirmation, empty for cancellation.");
                            switch (Console.ReadLine())
                            {
                                case "Y":
                                case "y":
                                    if (inventory[0] >= 3)
                                    {
                                        inventory[0] -= 3;
                                        Console.Clear();
                                        printHUD(rawChar, inventory, timeLocat);
                                        passTime(timeLocat, true, inventory, rawChar);
                                        txt = "  After refreshing nap you'll return back to the main floor of the 'Halfmoon Inn' and return the room key";
                                    }
                                    else
                                        txt = "  After you hear the price, you realize that you don't have enough Gold.";
                                    break;
                                default:
                                    txt = "  After you hear the price, you think you aren't sleepy enough to pay the fee and decide to do something else instead.";
                                    break;
                            }                            
                            eventPrintTxt(txt);
                            break;// sleep
                        case 3:
                            passTime(timeLocat, false, inventory, rawChar);
                            txt = "  You don't feel sleepy, but you'll need to wait a bit for optimal time. Atleast you don't have to wait alone since patrons seem quite chatty.";
                            eventPrintTxt(txt);
                            break;// wait
                        case 4:
                            if (info[0] == 0)
                            {
                                int spentTime = (rnd.Next(90 - (rawChar[6] + rawChar[4]) * 3, 180 - (rawChar[6] + rawChar[4]) * 3)); updateTimeAndReturnTxt(timeLocat, spentTime, rawChar, inventory); info[0] = 1; writeLineToSVFile(info, timeLocat[1]);
                                txt = "  You find a lot of rumors, though most of them seem quite useless. But some seem quite interesting." +
                                    "\n  You spent " + spentTime + " minutes to find the following info:\n";
                            }
                            else
                                txt = "  At quick glance there seems to be no new rumors around. And you still remember the old ones which are:\n";
                            txt += "\n  The treasure you are looking for seems to be considered as a myth so most don't believe it even exists," +
                                   "\n     some suggested that you might want investigate around Fel Mara since bigger city might have more leads." +
                                   "\n  Some claim that there has been sightings of thief group on the road to Fel Mara," +
                                   "\n     on the other hand some say that stopping to sight seeing might be dangerous, but highly profitable." +
                                   "\n  You also found out that Fel Fains smith is the best one this kingdom could offer," +
                                   "\n     but rumors say he's been having some issues getting the Voxor Ore for creating the best weapons.";
                            eventPrintTxt(txt);
                            break;// rumors
                        case 5:
                            Console.WriteLine("  As you approach the gamemaster, you notice weird plate that contains 3 spinners." +
                                "\n  Each of the spinners contains 4 different, weird and unrecognizable, symbols." +
                                "\n  You notice that after bets have been placed, gamemaster pours some liquid to the plate and spinners start to spin." +
                                "\n  After quick glance you estimate that each symbol has roughly same chance of appearing, though some seem luckier than others.\n\n  Press *Enter* to join in.");
                            Console.ReadLine();
                            bool cont = true; int winnings = 0; int luck = rawChar[6]; int bet;
                            if (luck < 0) luck = 0;
                            else if (luck > 5) luck = 5;
                            while (cont)
                            {
                                Console.Clear(); printHUD(rawChar, inventory, timeLocat);
                                Console.WriteLine("  Payout table: // §§§ = 20x // $$$ = 15x // £££ = 10x // %%% = 5x //\n");
                                Console.WriteLine("  Choose the size of the bet. You can bet from 1 to 100 Gold. Enter 0 to stop gambling.");
                                try
                                {
                                    bet = int.Parse(Console.ReadLine()); string result = "";
                                    if (bet == 0)
                                        cont = false;
                                    else if (bet >= 0 && bet <= 100)// normal (L3) // € = >0.75 - 1 // $ = >0.5 - 0.75 // £ = >0.25 - 0.5 // % = 0.0 - 0.25 //
                                    {
                                        if (inventory[0] >= bet)
                                        {
                                            winnings -= bet; inventory[0] -= bet;
                                            double rng1 = rnd.NextDouble(); double rng2 = rnd.NextDouble(); double rng3 = rnd.NextDouble();
                                            switch (luck)
                                            {
                                                case 0:// § = >0.75 - 1 // $ = >0.5 - 0.75 // £ = >0.25 - 0.5 // % = 0.0 - 0.25 //
                                                    // Spinner 1
                                                    if (rng1 <= 0.25) result = "%";
                                                    else if (rng1 > 0.25 && rng1 <= 0.50) result = "£";
                                                    else if (rng1 > 0.50 && rng1 <= 0.75) result = "$";
                                                    else if (rng1 > 0.75) result = "§";
                                                    // Spinner 2
                                                    if (rng2 <= 0.25) result += "%";
                                                    else if (rng2 > 0.25 && rng2 <= 0.50) result += "£";
                                                    else if (rng2 > 0.50 && rng2 <= 0.75) result += "$";
                                                    else if (rng2 > 0.75) result += "§";
                                                    // Spinner 3
                                                    if (rng3 <= 0.25) result += "%";
                                                    else if (rng3 > 0.25 && rng3 <= 0.50) result += "£";
                                                    else if (rng3 > 0.50 && rng3 <= 0.75) result += "$";
                                                    else if (rng3 > 0.75) result += "§";
                                                    // Force Loss
                                                    switch (result)
                                                    {
                                                        case "§§§":
                                                            result = "§§$";
                                                            break;
                                                        case "$$$":
                                                            result = "$$£";
                                                            break;
                                                        case "£££":
                                                            result = "££%";
                                                            break;
                                                        case "%%%":
                                                            result = "%%§";
                                                            break;
                                                        default:
                                                            break;
                                                    }
                                                    break;// luck 0
                                                case 1:// § = >0.15 - 1 // $ = >0.10 - 0.15 // £ = >0.05 - 0.10 // % = 0.0 - 0.05 //
                                                       // § = >0.75 - 1 // $ = >0.50 - 0.75 // £ = >0.25 - 0.50 // % = 0.0 - 0.25 //
                                                       // § = >0.95 - 1 // $ = >0.90 - 0.95 // £ = >0.85 - 0.90 // % = 0.0 - 0.85 //
                                                       // Spinner 1
                                                    if (rng1 <= 0.05) result = "%";
                                                    else if (rng1 > 0.05 && rng1 <= 0.10) result = "£";
                                                    else if (rng1 > 0.10 && rng1 <= 0.15) result = "$";
                                                    else if (rng1 > 0.15) result = "§";
                                                    // Spinner 2
                                                    if (rng2 <= 0.25) result += "%";
                                                    else if (rng2 > 0.25 && rng2 <= 0.50) result += "£";
                                                    else if (rng2 > 0.50 && rng2 <= 0.75) result += "$";
                                                    else if (rng2 > 0.75) result += "§";
                                                    // Spinner 3
                                                    if (rng3 <= 0.85) result += "%";
                                                    else if (rng3 > 0.85 && rng3 <= 0.90) result += "£";
                                                    else if (rng3 > 0.90 && rng3 <= 0.95) result += "$";
                                                    else if (rng3 > 0.95) result += "§";
                                                    break;// luck 1
                                                case 2:// § = >0.45 - 1 // $ = >0.30 - 0.45 // £ = >0.15 - 0.30 // % = 0.0 - 0.15 //
                                                       // § = >0.75 - 1 // $ = >0.50 - 0.75 // £ = >0.25 - 0.50 // % = 0.0 - 0.25 //
                                                       // § = >0.85 - 1 // $ = >0.70 - 0.85 // £ = >0.55 - 0.70 // % = 0.0 - 0.55 //
                                                       // Spinner 1
                                                    if (rng1 <= 0.15) result = "%";
                                                    else if (rng1 > 0.15 && rng1 <= 0.30) result = "£";
                                                    else if (rng1 > 0.30 && rng1 <= 0.45) result = "$";
                                                    else if (rng1 > 0.45) result = "§";
                                                    // Spinner 2
                                                    if (rng2 <= 0.25) result += "%";
                                                    else if (rng2 > 0.25 && rng2 <= 0.50) result += "£";
                                                    else if (rng2 > 0.50 && rng2 <= 0.75) result += "$";
                                                    else if (rng2 > 0.75) result += "§";
                                                    // Spinner 3
                                                    if (rng3 <= 0.25) result += "%";
                                                    else if (rng3 > 0.25 && rng3 <= 0.50) result += "£";
                                                    else if (rng3 > 0.50 && rng3 <= 0.75) result += "$";
                                                    else if (rng3 > 0.75) result += "§";
                                                    break;// luck 2
                                                case 3:// § = >0.75 - 1 // $ = >0.5 - 0.75 // £ = >0.25 - 0.5 // % = 0.0 - 0.25 //
                                                    // Spinner 1
                                                    if (rng1 <= 0.25) result = "%";
                                                    else if (rng1 > 0.25 && rng1 <= 0.50) result = "£";
                                                    else if (rng1 > 0.50 && rng1 <= 0.75) result = "$";
                                                    else if (rng1 > 0.75) result = "§";
                                                    // Spinner 2
                                                    if (rng2 <= 0.25) result += "%";
                                                    else if (rng2 > 0.25 && rng2 <= 0.50) result += "£";
                                                    else if (rng2 > 0.50 && rng2 <= 0.75) result += "$";
                                                    else if (rng2 > 0.75) result += "§";
                                                    // Spinner 3
                                                    if (rng3 <= 0.25) result += "%";
                                                    else if (rng3 > 0.25 && rng3 <= 0.50) result += "£";
                                                    else if (rng3 > 0.50 && rng3 <= 0.75) result += "$";
                                                    else if (rng3 > 0.75) result += "§";
                                                    break;// luck 3
                                                case 4:// § = >0.60 - 1 // $ = >0.40 - 0.60 // £ = >0.20 - 0.40 // % = 0.0 - 0.20 //
                                                    // Spinner 1
                                                    if (rng1 <= 0.20) result = "%";
                                                    else if (rng1 > 0.20 && rng1 <= 0.40) result = "£";
                                                    else if (rng1 > 0.40 && rng1 <= 0.60) result = "$";
                                                    else if (rng1 > 0.60) result = "§";
                                                    // Spinner 2
                                                    if (rng2 <= 0.25) result += "%";
                                                    else if (rng2 > 0.20 && rng2 <= 0.40) result += "£";
                                                    else if (rng2 > 0.40 && rng2 <= 0.60) result += "$";
                                                    else if (rng2 > 0.60) result += "§";
                                                    // Spinner 3
                                                    if (rng3 <= 0.25) result += "%";
                                                    else if (rng3 > 0.20 && rng3 <= 0.40) result += "£";
                                                    else if (rng3 > 0.40 && rng3 <= 0.60) result += "$";
                                                    else if (rng3 > 0.60) result += "§";
                                                    break;// luck 4
                                                case 5:// § = >0.45 - 1 // $ = >0.30 - 0.45 // £ = >0.15 - 0.30 // % = 0.0 - 0.15 //
                                                    // Spinner 1
                                                    if (rng1 <= 0.15) result = "%";
                                                    else if (rng1 > 0.15 && rng1 <= 0.30) result = "£";
                                                    else if (rng1 > 0.30 && rng1 <= 0.45) result = "$";
                                                    else if (rng1 > 0.45) result = "§";
                                                    // Spinner 2
                                                    if (rng2 <= 0.15) result += "%";
                                                    else if (rng2 > 0.15 && rng2 <= 0.30) result += "£";
                                                    else if (rng2 > 0.30 && rng2 <= 0.45) result += "$";
                                                    else if (rng2 > 0.45) result += "§";
                                                    // Spinner 3
                                                    if (rng3 <= 0.15) result += "%";
                                                    else if (rng3 > 0.15 && rng3 <= 0.30) result += "£";
                                                    else if (rng3 > 0.30 && rng3 <= 0.45) result += "$";
                                                    else if (rng3 > 0.45) result += "§";
                                                    break;// luck 5
                                            }
                                            Console.Clear(); printHUD(rawChar, inventory, timeLocat);
                                            Console.WriteLine("  Payout table: // §§§ = 20x // $$$ = 15x // £££ = 10x // %%% = 5x //\n");
                                            Console.WriteLine("  You got the following symbols // " + result + " //\n");
                                            switch (result)
                                            {
                                                case "§§§":// 20x
                                                    bet *= 20; inventory[0] += bet; winnings += bet;
                                                    Console.WriteLine("  So you Win " + bet + " Gold, Your current session balance is " + winnings + " Gold.\n  Press *Enter* to continue");
                                                    break;
                                                case "$$$":// 15x
                                                    bet *= 15; inventory[0] += bet; winnings += bet;
                                                    Console.WriteLine("  So you Win " + bet + " Gold, Your current session balance is " + winnings + " Gold.\n  Press *Enter* to continue");
                                                    break;
                                                case "£££":// 10x
                                                    bet *= 10; inventory[0] += bet; winnings += bet;
                                                    Console.WriteLine("  So you Win " + bet + " Gold, Your current session balance is " + winnings + " Gold.\n  Press *Enter* to continue");
                                                    break;
                                                case "%%%":// 5x
                                                    bet *= 5; inventory[0] += bet; winnings += bet;
                                                    Console.WriteLine("  So you Win " + bet + " Gold, Your current session balance is " + winnings + " Gold.\n  Press *Enter* to continue");
                                                    break;
                                                default:
                                                    Console.WriteLine("  So you Lost " + bet + " Gold, Your current session balance is " + winnings + " Gold.\n  Press *Enter* to continue");
                                                    break;
                                            }
                                            updateTimeAndReturnTxt(timeLocat, 3, rawChar, inventory);
                                            writeSaveFile(rawChar, inventory, timeLocat);
                                            Console.ReadLine();
                                        }
                                        else
                                            { Console.WriteLine("You don't have that much Gold, press *Enter* to try again"); Console.ReadLine(); }
                                    }
                                    else
                                        { Console.WriteLine("Invalid number, press *Enter* to try again"); Console.ReadLine(); }
                                }
                                catch
                                    { Console.WriteLine("Invalid Selection, press *Enter* to try again"); Console.ReadLine(); }
                            }
                            if (winnings > 0) txt = "  During this gambling session you Won total of " + winnings + " Gold.";
                            else if (winnings < 0) txt = "  During this gambling session you Lost total of " + winnings + " Gold.";
                            else txt = "  During this gambling session you broke even.";
                            eventPrintTxt(txt);
                            break;// gamble slots
                        case 6:
                            txt = "  TESTI TXT stealing";
                            eventPrintTxt(txt);
                            break;
                        case 7:// seduction or brawl
                            txt = "  TESTI TXT seduction or brawl";
                            eventPrintTxt(txt);
                            break;
                    }
                    break;// Inn (Fain) {heardRumor}
            }
            writeSaveFile(rawChar, inventory, timeLocat);
            writeLineToSVFile(status, 3);
            return deathQuit;
        }// Event Handler
    }
}

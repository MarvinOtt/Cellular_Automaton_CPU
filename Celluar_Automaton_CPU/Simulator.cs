using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Celluar_Automaton_CPU
{
    public class Simulator
    {
        private static List<Game1.int2> newactiveCells = new List<Game1.int2>();
        private static List<Game1.int2> changedCells = new List<Game1.int2>();
        public Simulator()
        {

        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateOneStep()
        {
            for (int i = 0; i < Game1.activeCells.Count; ++i)
            {
                // Update Cell only once per update
                if (Game1.AllreadyChanged[Game1.activeCells[i].x, Game1.activeCells[i].y] == 0)
                {
                    changedCells.Add(Game1.activeCells[i]);
                    Game1.AllreadyChanged[Game1.activeCells[i].x, Game1.activeCells[i].y] = 1;
                    UpdateCell(Game1.activeCells[i]);
                }
                /*else
                {
                    Game1.activeCells.RemoveAt(i);
                    i--;
                }*/
            }

            //Parallel.For(0, Game1.activeCells.Count, i =>

            for (int i = 0; i < changedCells.Count; ++i)
            {
                Game1.layer1_values[changedCells[i].x, changedCells[i].y] = Game1.layer1_newvalues[changedCells[i].x, changedCells[i].y];
            }


            for (int i = 0; i < Game1.activeCells.Count; ++i)
            {
                //Calculatenewactivecells(Game1.activeCells[i]);
                Calculateallnewactivecells(Game1.activeCells[i]);
            };

            Game1.activeCells.Clear();
            Game1.activeCells = new List<Game1.int2>(newactiveCells);
            newactiveCells.Clear();

            for (int i = 0; i < changedCells.Count; ++i)
            {
                //byte value = Game1.AllreadyChanged[changedCells[i].x, changedCells[i].y];
                Game1.AllreadyChanged[changedCells[i].x, changedCells[i].y] = 0;
            }
            changedCells.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateCell(Game1.int2 position)
        {
            int x = position.x, y = position.y;
            int PORTAL;
            byte left_type = Game1.layer1_values[position.x - 1, position.y];
            byte right_type = Game1.layer1_values[position.x + 1, position.y];
            byte top_type = Game1.layer1_values[position.x, position.y - 1];
            byte bottom_type = Game1.layer1_values[position.x, position.y + 1];
            byte current_type = Game1.layer1_values[position.x, position.y];

            byte OUT = 0;
            byte current_strength = 0;
            if (Game1.layer1_values[position.x, position.y] > 4 && Game1.layer1_values[position.x, position.y] < 10)
            {
                return;
            }

            // Normal Particles
            if (left_type == 2 || (left_type == 6 && Game1.layer1_values[position.x - 2, position.y] == 2))
            {
                current_strength++;
                OUT = 2;
            }
            if (bottom_type == 1 || (bottom_type == 6 && Game1.layer1_values[position.x, position.y + 2] == 1))
            {
                current_strength++;
                OUT = 1;
            }
            if (right_type == 4 || (right_type == 6 && Game1.layer1_values[position.x + 2, position.y] == 4))
            {
                current_strength++;
                OUT = 4;
            }
            if (top_type == 3 || (top_type == 6 && Game1.layer1_values[position.x, position.y - 2] == 3))
            {
                current_strength++;
                OUT = 3;
            }

            if (Game1.layer2_values[x, y] > 4) // Output Block
            {
                if (Game1.layer2_values[x, y] < 9) // Normal Output Blocks
                {
                    // Energie
                    if (top_type > 9 && top_type < 12 && Game1.layer2_values[x, y] != 5) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type > 9 && right_type < 12 && Game1.layer2_values[x, y] != 6) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type > 9 && bottom_type < 12 && Game1.layer2_values[x, y] != 7) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type > 9 && left_type < 12 && Game1.layer2_values[x, y] != 8) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                    // AND
                    if (top_type == 7 && Game1.layer2_values[x, y] != 5 && Gate_Calculation(x, y - 1) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type == 7 && Game1.layer2_values[x, y] != 6 && Gate_Calculation(x + 1, y) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type == 7 && Game1.layer2_values[x, y] != 7 && Gate_Calculation(x, y + 1) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type == 7 && Game1.layer2_values[x, y] != 8 && Gate_Calculation(x - 1, y) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                    // OR
                    if (top_type == 8 && Game1.layer2_values[x, y] != 5 && Gate_Calculation(x, y - 1) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type == 8 && Game1.layer2_values[x, y] != 6 && Gate_Calculation(x + 1, y) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type == 8 && Game1.layer2_values[x, y] != 7 && Gate_Calculation(x, y + 1) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type == 8 && Game1.layer2_values[x, y] != 8 && Gate_Calculation(x - 1, y) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                    // XOR
                    if (top_type == 9 && Game1.layer2_values[x, y] != 5 && Gate_Calculation(x, y - 1) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type == 9 && Game1.layer2_values[x, y] != 6 && Gate_Calculation(x + 1, y) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type == 9 && Game1.layer2_values[x, y] != 7 && Gate_Calculation(x, y + 1) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type == 9 && Game1.layer2_values[x, y] != 8 && Gate_Calculation(x - 1, y) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                }
                else // Inverted Output Blocks
                {
                    // Energie
                    if (top_type > 11 && Game1.layer2_values[x, y] != 9) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type > 11 && Game1.layer2_values[x, y] != 10) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type > 11 && Game1.layer2_values[x, y] != 11) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type > 11 && Game1.layer2_values[x, y] != 12) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }

                    // NAND
                    if (top_type == 7 && Game1.layer2_values[x, y] != 9 && Gate_Calculation(x, y - 1) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type == 7 && Game1.layer2_values[x, y] != 10 && Gate_Calculation(x + 1, y) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type == 7 && Game1.layer2_values[x, y] != 11 && Gate_Calculation(x, y + 1) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type == 7 && Game1.layer2_values[x, y] != 12 && Gate_Calculation(x - 1, y) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }

                    // NOR
                    if (top_type == 8 && Game1.layer2_values[x, y] != 9 && Gate_Calculation(x, y - 1) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type == 8 && Game1.layer2_values[x, y] != 10 && Gate_Calculation(x + 1, y) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type == 8 && Game1.layer2_values[x, y] != 11 && Gate_Calculation(x, y + 1) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type == 8 && Game1.layer2_values[x, y] != 12 && Gate_Calculation(x - 1, y) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }

                    // XNOR
                    if (top_type == 9 && Game1.layer2_values[x, y] != 9 && Gate_Calculation(x, y - 1) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type == 9 && Game1.layer2_values[x, y] != 10 && Gate_Calculation(x + 1, y) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type == 9 && Game1.layer2_values[x, y] != 11 && Gate_Calculation(x, y + 1) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type == 9 && Game1.layer2_values[x, y] != 12 && Gate_Calculation(x - 1, y) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                }
            }

            // Energie Cells
            if (current_type >= 10 && current_type <= 13)
            {
                // 7: without energie
                // 8: no energie impuls
                // 9: with energie
                // 10: energie impuls
                current_strength = 1;
                int counter = 0;
                if (top_type == 3)
                    counter++;
                if (bottom_type == 1)
                    counter++;
                if (right_type == 4)
                    counter++;
                if (left_type == 2)
                    counter++;
                if ((left_type == 6 && right_type == 6) || (top_type == 6 && bottom_type == 6))
                {
                    if (counter >= 1)
                    {
                        if (current_type == 12 || current_type == 13)
                            OUT = 10; // energie impuls
                        else
                            OUT = 13; // energie impuls
                        Game1.PossibleCellIndexChange.Add(position);
                        goto PORTAL;
                    }
                }

                if (counter > 1)
                    OUT = 11; // energie impuls
                else if (counter == 1)
                    OUT = 13; // Without Energy
                else if (current_type == 12) // no energie
                {
                    if (right_type == 11 || left_type == 11 || bottom_type == 11 || top_type == 11)
                        OUT = 11;
                    else
                        OUT = current_type;
                }
                else if (current_type == 10) // with energie
                {
                    if (right_type == 13 || left_type == 13 || bottom_type == 13 || top_type == 13)
                        OUT = 13;
                    else
                        OUT = current_type;
                }
                else if (current_type == 13)
                    OUT = 12;
                else if (current_type == 11)
                    OUT = 10;
                else
                    OUT = current_type;
            }


            Game1.PossibleCellIndexChange.Add(position);
        PORTAL:
            if (current_strength == 1)
            {

                if (Game1.layer2_values[position.x, position.y] == 0 || Game1.layer2_values[position.x, position.y] > 4) // Layer2 is not direction pads
                    Game1.layer1_newvalues[position.x, position.y] = OUT;
                else
                    Game1.layer1_newvalues[position.x, position.y] = Game1.layer2_values[position.x, position.y];
            }
            else
                Game1.layer1_newvalues[position.x, position.y] = 0;
            //Calculatenewactivecells(position);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Gate_Calculation(int x, int y)
        {
            byte counter = 0;
            if (Game1.layer1_values[x, y - 1] == 3 && (Game1.layer2_values[x, y - 1] < 5 || Game1.layer2_values[x, y - 1] == 7 || Game1.layer2_values[x, y - 1] == 11))
                counter++;
            if (Game1.layer1_values[x + 1, y] == 4 && (Game1.layer2_values[x + 1, y] < 5 || Game1.layer2_values[x + 1, y] == 8 || Game1.layer2_values[x + 1, y] == 12))
                counter++;
            if (Game1.layer1_values[x, y + 1] == 1 && (Game1.layer2_values[x, y + 1] < 5 || Game1.layer2_values[x, y + 1] == 5 || Game1.layer2_values[x, y + 1] == 9))
                counter++;
            if (Game1.layer1_values[x - 1, y] == 2 && (Game1.layer2_values[x - 1, y] < 5 || Game1.layer2_values[x - 1, y] == 6 || Game1.layer2_values[x - 1, y] == 10))
                counter++;
            return counter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Calculateactivecells4Gates(int x, int y)
        {
            if (Game1.layer2_values[x, y - 1] > 4)
                Calculateonenewactivecell(x, y - 1, Game1.layer1_values[x - 1, y - 1], Game1.layer1_values[x + 1, y - 1], Game1.layer1_values[x, y - 2], Game1.layer1_values[x, y], Game1.layer1_values[x, y - 1]);
            if (Game1.layer2_values[x + 1, y] > 4)
                Calculateonenewactivecell(x + 1, y, Game1.layer1_values[x, y], Game1.layer1_values[x + 2, y], Game1.layer1_values[x + 1, y - 1], Game1.layer1_values[x + 1, y + 1], Game1.layer1_values[x + 1, y]);
            if (Game1.layer2_values[x, y + 1] > 4)
                Calculateonenewactivecell(x, y + 1, Game1.layer1_values[x - 1, y + 1], Game1.layer1_values[x + 1, y + 1], Game1.layer1_values[x, y], Game1.layer1_values[x, y + 2], Game1.layer1_values[x, y + 1]);
            if (Game1.layer2_values[x - 1, y] > 4)
                Calculateonenewactivecell(x - 1, y, Game1.layer1_values[x - 2, y], Game1.layer1_values[x, y], Game1.layer1_values[x - 1, y - 1], Game1.layer1_values[x - 1, y + 1], Game1.layer1_values[x - 1, y]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Calculateallnewactivecells(Game1.int2 position)
        {
            int x = position.x, y = position.y;
            int posxp1 = x + 1;
            int posyp1 = y + 1;
            int posxm1 = x - 1;
            int posym1 = y - 1;

            byte type_0_0 = Game1.layer1_newvalues[x, y];
            byte type_0_m1 = Game1.layer1_newvalues[x, posym1];
            byte type_1_0 = Game1.layer1_newvalues[posxp1, y];
            byte type_0_1 = Game1.layer1_newvalues[x, posyp1];
            byte type_m1_0 = Game1.layer1_newvalues[posxm1, y];

            byte type_0_m2 = Game1.layer1_newvalues[x, y - 2];
            byte type_2_0 = Game1.layer1_newvalues[x + 2, y];
            byte type_0_2 = Game1.layer1_newvalues[x, y + 2];
            byte type_m2_0 = Game1.layer1_newvalues[x - 2, y];

            byte type_1_m1 = Game1.layer1_newvalues[posxp1, posym1];
            byte type_1_1 = Game1.layer1_newvalues[posxp1, posyp1];
            byte type_m1_1 = Game1.layer1_newvalues[posxm1, posyp1];
            byte type_m1_m1 = Game1.layer1_newvalues[posxm1, posym1];
            if (type_0_0 < 5 || type_0_0 > 9)
                Calculateonenewactivecell(x, y, type_m1_0, type_1_0, type_0_m1, type_0_1, type_0_0);

            if (type_0_m1 < 5 || type_0_m1 > 9) { Calculateonenewactivecell(x, posym1, type_m1_m1, type_1_m1, type_0_m2, type_0_0, type_0_m1); }
            else if (type_0_m1 == 6 && Game1.layer1_newvalues[x, y - 2] < 5) // Jumper
                Calculateonenewactivecell(x, y - 2, Game1.layer1_newvalues[x - 1, y - 2], Game1.layer1_newvalues[x + 1, y - 2], Game1.layer1_newvalues[x, y - 3], type_0_m1, Game1.layer1_newvalues[x, y - 2]);
            else if (type_0_m1 != 5) // Gates
                Calculateactivecells4Gates(x, posym1);

            if (type_0_1 < 5 || type_0_1 > 9) { Calculateonenewactivecell(x, posyp1, type_m1_1, type_1_1, type_0_0, type_0_2, type_0_1); }
            else if (type_0_1 == 6 && Game1.layer1_newvalues[x, y + 2] < 5) // Jumper
                Calculateonenewactivecell(x, y + 2, Game1.layer1_newvalues[x - 1, y + 2], Game1.layer1_newvalues[x + 1, y + 2], type_0_1, Game1.layer1_newvalues[x, y + 3], Game1.layer1_newvalues[x, y + 2]);
            else if (type_0_1 != 5) // Gates
                Calculateactivecells4Gates(x, posyp1);

            if (type_m1_0 < 5 || type_m1_0 > 9) { Calculateonenewactivecell(posxm1, y, type_m2_0, type_0_0, type_m1_m1, type_m1_1, type_m1_0); }
            else if (type_m1_0 == 6 && Game1.layer1_newvalues[x - 2, y] < 5) // Jumper
                Calculateonenewactivecell(x - 2, y, Game1.layer1_newvalues[x - 3, y], type_m1_0, Game1.layer1_newvalues[x - 2, y - 1], Game1.layer1_newvalues[x - 2, y + 1], Game1.layer1_newvalues[x - 2, y]);
            else if (type_m1_0 != 5) // Gates
                Calculateactivecells4Gates(posxm1, y);

            if (type_1_0 < 5 || type_1_0 > 9) { Calculateonenewactivecell(posxp1, y, type_0_0, type_2_0, type_1_m1, type_1_1, type_1_0); }
            else if (type_1_0 == 6 && Game1.layer1_newvalues[x + 2, y] < 5) // Jumper
                Calculateonenewactivecell(x + 2, y, type_1_0, Game1.layer1_newvalues[x + 3, y], Game1.layer1_newvalues[x + 2, y - 1], Game1.layer1_newvalues[x + 2, y + 1], Game1.layer1_newvalues[x + 2, y]);
            else if (type_1_0 != 5) // Gates
                Calculateactivecells4Gates(posxp1, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Calculateonenewactivecell(int x, int y, byte left_type, byte right_type, byte top_type, byte bottom_type, byte current_type)
        {
            if (Game1.AllreadyChanged[x, y] == 2)
                return;
            int PORTAL;
            if (x < 2 || y < 2 || x > Game1.sizex - 3 || y == Game1.sizey - 3)
                return;

            /*byte left_type = Game1.layer1_newvalues[position.x - 1, position.y];
            byte right_type = Game1.layer1_newvalues[position.x + 1, position.y];
            byte top_type = Game1.layer1_newvalues[position.x, position.y - 1];
            byte bottom_type = Game1.layer1_newvalues[position.x, position.y + 1];
            byte current_type = Game1.layer1_newvalues[position.x, position.y];*/
            byte OUT = 0;
            byte current_strength = 0, outputcell_strength = 0;

            /*if (Game1.layer1_newvalues[x, y] > 4)
            {
                OUT = Game1.layer1_newvalues[x, y];
                current_strength = 1;
                goto PORTAL;
            }*/

            // Normal Particles
            if (left_type == 2 || (left_type == 6 && Game1.layer1_newvalues[x - 2, y] == 2))
            {
                current_strength++;
                OUT = 2;
            }
            if (bottom_type == 1 || (bottom_type == 6 && Game1.layer1_newvalues[x, y + 2] == 1))
            {
                current_strength++;
                OUT = 1;
            }
            if (right_type == 4 || (right_type == 6 && Game1.layer1_newvalues[x + 2, y] == 4))
            {
                current_strength++;
                OUT = 4;
            }
            if (top_type == 3 || (top_type == 6 && Game1.layer1_newvalues[x, y - 2] == 3))
            {
                current_strength++;
                OUT = 3;
            }

            if (Game1.layer2_values[x, y] > 4) // Output Block
            {
                if (Game1.layer2_values[x, y] < 9) // Normal Output Blocks
                {
                    // Energie
                    if (top_type > 9 && top_type < 12 && Game1.layer2_values[x, y] != 5) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type > 9 && right_type < 12 && Game1.layer2_values[x, y] != 6) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type > 9 && bottom_type < 12 && Game1.layer2_values[x, y] != 7) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type > 9 && left_type < 12 && Game1.layer2_values[x, y] != 8) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                    // AND
                    if (top_type == 7 && Game1.layer2_values[x, y] != 5 && Gate_Calculation(x, y - 1) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type == 7 && Game1.layer2_values[x, y] != 6 && Gate_Calculation(x + 1, y) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type == 7 && Game1.layer2_values[x, y] != 7 && Gate_Calculation(x, y + 1) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type == 7 && Game1.layer2_values[x, y] != 8 && Gate_Calculation(x - 1, y) > 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                    // OR
                    if (top_type == 8 && Game1.layer2_values[x, y] != 5 && Gate_Calculation(x, y - 1) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type == 8 && Game1.layer2_values[x, y] != 6 && Gate_Calculation(x + 1, y) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type == 8 && Game1.layer2_values[x, y] != 7 && Gate_Calculation(x, y + 1) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type == 8 && Game1.layer2_values[x, y] != 8 && Gate_Calculation(x - 1, y) > 0) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                    // XOR
                    if (top_type == 9 && Game1.layer2_values[x, y] != 5 && Gate_Calculation(x, y - 1) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (right_type == 9 && Game1.layer2_values[x, y] != 6 && Gate_Calculation(x + 1, y) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (bottom_type == 9 && Game1.layer2_values[x, y] != 7 && Gate_Calculation(x, y + 1) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }
                    if (left_type == 9 && Game1.layer2_values[x, y] != 8 && Gate_Calculation(x - 1, y) % 2 == 1) { OUT = (byte)(Game1.layer2_values[x, y] - 4); current_strength++; }

                }
                else // Inverted Output Blocks
                {
                    // Energie
                    if (top_type > 11 && Game1.layer2_values[x, y] != 9) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type > 11 && Game1.layer2_values[x, y] != 10) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type > 11 && Game1.layer2_values[x, y] != 11) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type > 11 && Game1.layer2_values[x, y] != 12) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }

                    // NAND
                    if (top_type == 7 && Game1.layer2_values[x, y] != 9 && Gate_Calculation(x, y - 1) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type == 7 && Game1.layer2_values[x, y] != 10 && Gate_Calculation(x + 1, y) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type == 7 && Game1.layer2_values[x, y] != 11 && Gate_Calculation(x, y + 1) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type == 7 && Game1.layer2_values[x, y] != 12 && Gate_Calculation(x - 1, y) < 2) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }

                    // NOR
                    if (top_type == 8 && Game1.layer2_values[x, y] != 9 && Gate_Calculation(x, y - 1) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type == 8 && Game1.layer2_values[x, y] != 10 && Gate_Calculation(x + 1, y) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type == 8 && Game1.layer2_values[x, y] != 11 && Gate_Calculation(x, y + 1) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type == 8 && Game1.layer2_values[x, y] != 12 && Gate_Calculation(x - 1, y) == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }

                    // XNOR
                    if (top_type == 9 && Game1.layer2_values[x, y] != 9 && Gate_Calculation(x, y - 1) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (right_type == 9 && Game1.layer2_values[x, y] != 10 && Gate_Calculation(x + 1, y) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (bottom_type == 9 && Game1.layer2_values[x, y] != 11 && Gate_Calculation(x, y + 1) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                    if (left_type == 9 && Game1.layer2_values[x, y] != 12 && Gate_Calculation(x - 1, y) % 2 == 0) { OUT = (byte)(Game1.layer2_values[x, y] - 8); current_strength++; }
                }
            }

            // Energie Cells
            if (current_type >= 10 && current_type <= 13)
            {
                // 7: without energie
                // 8: no energie impuls
                // 9: with energie
                // 10: energie impuls
                current_strength = 1;
                int counter = 0;
                if (top_type == 3)
                    counter++;
                if (bottom_type == 1)
                    counter++;
                if (right_type == 4)
                    counter++;
                if (left_type == 2)
                    counter++;
                if ((left_type == 6 && right_type == 6) || (top_type == 6 && bottom_type == 6))
                {
                    if (counter > 0)
                    {
                        if (current_type == 12 || current_type == 13)
                            OUT = 11; // energie impuls
                        else
                            OUT = 13; // energie impuls
                        goto PORTAL;
                    }
                }

                if (counter > 1)
                    OUT = 11; // energie impuls
                else if (counter == 1)
                    OUT = 13; // Without Energy
                else if (current_type == 12) // no energie
                {
                    if (right_type == 11 || left_type == 11 || bottom_type == 11 || top_type == 11)
                        OUT = 11;
                    else
                        OUT = current_type;
                }
                else if (current_type == 10) // with energie
                {
                    if (right_type == 13 || left_type == 13 || bottom_type == 13 || top_type == 13)
                        OUT = 13;
                    else
                        OUT = current_type;
                }
                else if (current_type == 13)
                    OUT = 12;
                else if (current_type == 11)
                    OUT = 10;
                else
                    OUT = current_type;
            }


        PORTAL:
            if (current_strength == 1)
            {
                if (Game1.layer2_values[x, y] > 0 && Game1.layer2_values[x, y] < 5)
                {
                    OUT = Game1.layer2_values[x, y];
                }
            }
            else
                OUT = 0;
            if (OUT != current_type)
            {
                newactiveCells.Add(new Game1.int2(x, y));
                if (Game1.AllreadyChanged[x, y] == 0)
                    changedCells.Add(new Game1.int2(x, y));
                Game1.AllreadyChanged[x, y] = 2;
            }
        }
    }
}

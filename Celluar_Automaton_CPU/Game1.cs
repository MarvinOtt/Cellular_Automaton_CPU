using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using FormClosingEventArgs = System.Windows.Forms.FormClosingEventArgs;
using Form = System.Windows.Forms.Form;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Celluar_Automaton_CPU
{
    public class Game1 : Game
    {
        public static int _count;

        public struct int2
        {
            public int x, y;

            public int2(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        GraphicsDeviceManager graphics;
        public static ContentManager contmanager;
        SpriteBatch spriteBatch;
        SpriteFont font;
        public static Random r = new Random();
        private KeyboardState KB_currentstate, KB_oldstate;
        private MouseState M_currentstate, M_oldstate;
        public static int size_mul16x = 1024;
        public static int size_mul16y = 1024;
        public static int sizex = size_mul16x * 16;
        public static int sizey = size_mul16y * 16;

        // Textures
        public static Texture2D logictex;
        public static Texture2D outputtex;
        private Texture2D CopyTexture;
        private Texture2D button_play, button_break, button_reset;

        // Lists and Arrays
        public static byte[,] layer1_values, layer1_oldvalues, layer2_values, layer1_newvalues, AllreadyChanged, generation0_values;
        public static byte[,] CopiedCells = null;
        public static short[,] UpdateTex16x16;
        public static bool[,] MayHaveActiveCells, HasChangedsincegen0;
        public static int2[,] UpdateTex16x16_onepixpos;
        private List<int2> UpdateTex16x16_List;
        public static List<int2> activeCells;
        public static List<int2> PossibleCellIndexChange;
        public static List<int2> NeedCheck4ActiveState;
        List<Element> layer1_elements = new List<Element>();
        List<Element> layer2_elements = new List<Element>();
        public static LinkedList<int2> generation0changed = new LinkedList<int2>();
        public static Element[] overlays = new Element[12];

        // Other Variables
        private byte currentcellindex = 18;
        private int worldzoom = 0;
        private Vector2 worldpos;
        private int simulationspeed = 6, simulationtimer;
        private static long currentsimulationstep = 0;
        private bool IsSimulating = false;
        private static Simulator simulator;
        private static int newaddedelemntscounter = 0;
        private int worldposx_old, worldposy_old;
        private float time1;
        private static int setanz = 0;

        #region Selection Variables

        private static int currentselectiontype = 0;
        private int2 Selection_Start, Selection_End, Selection_Start_total, Selection_End_total;
        private bool IsSelecting = false;
        private int2 copiedpos;
        private bool IsCopyMoving = false;

        #endregion


        private Effect renderEffect;


        public static int Screenwidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        public static int Screenheight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height,
                IsFullScreen = false,
                SynchronizeWithVerticalRetrace = true

            };
            IsMouseVisible = true;
            IsFixedTimeStep = false;
            Window.IsBorderless = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Form f = Form.FromHandle(Window.Handle) as Form;
            f.Location = new System.Drawing.Point(0, 0);
            if (f != null) { f.FormClosing += f_FormClosing; }
            base.Initialize();
        }

        private void f_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Exit();
            Thread.Sleep(100);
            base.Exit();
        }
        public void screen2worldcoo_int(Vector2 screencoos, out int x, out int y)
        {
            x = (int)((screencoos.X - worldpos.X) / (float)Math.Pow(2, worldzoom));
            y = (int)((screencoos.Y - worldpos.Y) / (float)Math.Pow(2, worldzoom));
        }
        public Vector2 screen2worldcoo_Vector2(Vector2 screencoos)
        {
            Vector2 OUT;
            OUT.X = ((screencoos.X - worldpos.X) / (float)Math.Pow(2, worldzoom));
            OUT.Y = ((screencoos.Y - worldpos.Y) / (float)Math.Pow(2, worldzoom));
            return OUT;
        }
        public static byte[] color = new byte[256];
        public static void SetPixel_16x16(int x, int y, Texture2D tex)
        {
            Rectangle r = new Rectangle(x * 16, y * 16, 16, 16);
            for (int x2 = 0; x2 < 16; ++x2)
            {
                for (int y2 = 0; y2 < 16; ++y2)
                {
                    color[x2 + y2 * 16] = (byte)(layer2_values[x * 16 + x2, y * 16 + y2] * 17 + layer1_values[x * 16 + x2, y * 16 + y2]);
                }
            }
            //setanz++;
            tex.SetData<byte>(0, r, color, 0, 256);
        }
        public static void SetPixel_1x1(int x, int y, Texture2D tex)
        {
            Rectangle r = new Rectangle(x, y, 1, 1);
            byte[] color2 = new byte[1] { (byte)(layer2_values[x, y] * 17 + layer1_values[x, y])};
            setanz++;
            tex.SetData<byte>(0, r, color2, 0, 1);
        }
        public static void SetGraphicsDevice_CellValues(byte value_layer1, byte value_layer2, int x, int y)
        {
            byte finalvalue = (byte)(value_layer2 * 17 + value_layer1);
            //SetPixel(x, y, finalvalue, logictex);
        }

        public static void Set_CellValues(byte value_layer1, byte value_layer2, int x, int y)
        {
            byte val1 = layer1_values[x, y];
            byte val2 = layer2_values[x, y];

            if (layer1_values[x, y] != value_layer1 || layer2_values[x, y] != value_layer2)
            {
                layer1_values[x, y] = layer1_newvalues[x, y] = value_layer1;
                int oldlayer2value = layer2_values[x, y];
                layer2_values[x, y] = value_layer2;

                if (oldlayer2value != 0 && value_layer2 == 0)
                    SetPixel_1x1(x, y, logictex);
                //Checkfornewactivecells(new int2(x, y));
                PossibleCellIndexChange.Add(new int2(x, y));
                NeedCheck4ActiveState.Add(new int2(x, y));
                MayHaveActiveCells[x / 16, y / 16] = true;
            }
        }
        public static void UpdateActiveCells()
        {
            for (int i = 0; i < activeCells.Count; ++i)
            {
                int x = activeCells[i].x;
                int y = activeCells[i].y;
                if (x < 2 || y < 2 || x > sizex - 3 || y == sizey - 3)
                {
                    activeCells.RemoveAt(i);
                    i--;
                }
            }
        }

        public static void RemoveDuplicatefromLinkedList<T>(LinkedList<T> input)
        {
            HashSet<T> set = new HashSet<T>();
            for (LinkedListNode<T> it = input.First; it != null;)
            {
                if (!set.Add(it.Value))
                {
                    LinkedListNode<T> next = it.Next;
                    input.Remove(it);
                    it = next;
                }
                else
                    it = it.Next;
            }
        }
        public static void Removeunnecessarygeneration0changes()
        {
            for (LinkedListNode<int2> it = generation0changed.First; it != null;)
            {
                int x = it.Value.x;
                int y = it.Value.y;
                if (generation0_values[x, y] == layer2_values[x, y] * 17 + layer1_values[x, y])
                {
                    LinkedListNode<int2> next = it.Next;
                    generation0changed.Remove(it);
                    HasChangedsincegen0[it.Value.x, it.Value.y] = false;
                    it = next;
                }
                else
                    it = it.Next;
            }
        }
        public static byte Gate_Calculation(int x, int y)
        {
            byte counter = 0;
            if ((Game1.layer2_values[x, y - 1] < 5 || Game1.layer2_values[x, y - 1] == 7 || Game1.layer2_values[x, y - 1] == 11) && Game1.layer1_values[x, y - 1] == 3)
                counter++;
            if ((Game1.layer2_values[x + 1, y] < 5 || Game1.layer2_values[x + 1, y] == 8 || Game1.layer2_values[x + 1, y] == 12) && Game1.layer1_values[x + 1, y] == 4)
                counter++;
            if ((Game1.layer2_values[x, y + 1] < 5 || Game1.layer2_values[x, y + 1] == 5 || Game1.layer2_values[x, y + 1] == 9) && Game1.layer1_values[x, y + 1] == 1)
                counter++;
            if ((Game1.layer2_values[x - 1, y] < 5 || Game1.layer2_values[x - 1, y] == 6 || Game1.layer2_values[x - 1, y] == 10) && Game1.layer1_values[x - 1, y] == 2)
                counter++;
            return counter;
        }
        public static void Calculateactivecells4Gates(int x, int y)
        {
            if (Game1.layer2_values[x, y - 1] > 4 && Game1.layer2_values[x, y - 1] != 7 && Game1.layer2_values[x, y - 1] != 11)
                Calculateonenewactivecell(x, y - 1, Game1.layer1_values[x - 1, y - 1], Game1.layer1_values[x + 1, y - 1], Game1.layer1_values[x, y - 2], Game1.layer1_values[x, y], Game1.layer1_values[x, y - 1]);
            if (Game1.layer2_values[x + 1, y] > 4 && Game1.layer2_values[x + 1, y] != 8 && Game1.layer2_values[x + 1, y] != 12)
                Calculateonenewactivecell(x + 1, y, Game1.layer1_values[x, y], Game1.layer1_values[x + 2, y], Game1.layer1_values[x + 1, y - 1], Game1.layer1_values[x + 1, y + 1], Game1.layer1_values[x + 1, y]);
            if (Game1.layer2_values[x, y + 1] > 4 && Game1.layer2_values[x, y + 1] != 5 && Game1.layer2_values[x, y + 1] != 9)
                Calculateonenewactivecell(x, y + 1, Game1.layer1_values[x - 1, y + 1], Game1.layer1_values[x + 1, y + 1], Game1.layer1_values[x, y], Game1.layer1_values[x, y + 2], Game1.layer1_values[x, y + 1]);
            if (Game1.layer2_values[x - 1, y] > 4 && Game1.layer2_values[x - 1, y] != 6 && Game1.layer2_values[x - 1, y] != 10)
                Calculateonenewactivecell(x - 1, y, Game1.layer1_values[x - 2, y], Game1.layer1_values[x, y], Game1.layer1_values[x - 1, y - 1], Game1.layer1_values[x - 1, y + 1], Game1.layer1_values[x - 1, y]);
        }
        public static void Checkfornewactivecells(Game1.int2 position)
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
        public static void Calculateonenewactivecell(int x, int y, byte left_type, byte right_type, byte top_type, byte bottom_type, byte current_type)
        {
            int PORTAL;

            /*byte left_type = Game1.layer1_newvalues[position.x - 1, position.y];
            byte right_type = Game1.layer1_newvalues[position.x + 1, position.y];
            byte top_type = Game1.layer1_newvalues[position.x, position.y - 1];
            byte bottom_type = Game1.layer1_newvalues[position.x, position.y + 1];
            byte current_type = Game1.layer1_newvalues[position.x, position.y];*/
            byte OUT = 0;
            byte current_strength = 0, outputcell_strength = 0;

            if (x < 2 || y < 2 || x > Game1.sizex - 3 || y == Game1.sizey - 3)
                return;

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
                activeCells.Add(new Game1.int2(x, y));
            }
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            contmanager = Content;
            simulator = new Simulator();

            font = Content.Load<SpriteFont>("font");
            renderEffect = Content.Load<Effect>("render_effect");
            renderEffect.Parameters["Screenwidth"].SetValue(Screenwidth);
            renderEffect.Parameters["Screenheight"].SetValue(Screenheight);
            renderEffect.Parameters["worldsizex"].SetValue(sizex);
            renderEffect.Parameters["worldsizey"].SetValue(sizey);

            #region Loading UI

            button_break = Content.Load<Texture2D>("button_break");
            button_play = Content.Load<Texture2D>("button_play");
            button_reset = Content.Load<Texture2D>("button_reset");
            layer1_elements.Add(new Element("button_partikel_top", 1));
            layer1_elements.Add(new Element("button_partikel_right", 2));
            layer1_elements.Add(new Element("button_partikel_bottom", 3));
            layer1_elements.Add(new Element("button_partikel_left", 4));
            layer1_elements.Add(new Element("button_partikel_multifunction", 5));
            layer1_elements.Add(new Element("button_partikel_blue", 6));
            layer1_elements.Add(new Element("button_partikel_AND", 7));
            layer1_elements.Add(new Element("button_partikel_OR", 8));
            layer1_elements.Add(new Element("button_partikel_XOR", 9));
            layer1_elements.Add(new Element("button_partikel_highred", 10));
            layer1_elements.Add(new Element("button_partikel_lowred", 12));

            layer2_elements.Add(new Element("Direction Changer UP", 14));
            layer2_elements.Add(new Element("Direction Changer RIGHT", 15));
            layer2_elements.Add(new Element("Direction Changer DOWN", 16));
            layer2_elements.Add(new Element("Direction Changer LEFT", 17));
            layer2_elements.Add(new Element("Output UP", 18));
            layer2_elements.Add(new Element("Output RIGHT", 19));
            layer2_elements.Add(new Element("Output DOWN", 20));
            layer2_elements.Add(new Element("Output LEFT", 21));
            layer2_elements.Add(new Element("Output INV UP", 22));
            layer2_elements.Add(new Element("Output INV RIGHT", 23));
            layer2_elements.Add(new Element("Output INV DOWN", 24));
            layer2_elements.Add(new Element("Output INV LEFT", 25));

            overlays[0] = new Element(".\\Cell Overlays\\OVERLAY DC UP", 1);
            overlays[1] = new Element(".\\Cell Overlays\\OVERLAY DC RIGHT", 2);
            overlays[2] = new Element(".\\Cell Overlays\\OVERLAY DC DOWN", 3);
            overlays[3] = new Element(".\\Cell Overlays\\OVERLAY DC LEFT", 4);
            overlays[4] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT UP", 5);
            overlays[5] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT RIGHT", 6);
            overlays[6] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT DOWN", 7);
            overlays[7] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT LEFT", 8);
            overlays[8] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT INV UP", 9);
            overlays[9] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT INV RIGHT", 10);
            overlays[10] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT INV DOWN", 11);
            overlays[11] = new Element(".\\Cell Overlays\\OVERLAY OUTPUT INV LEFT", 12);

            #endregion

            // Inizialising Arrays and Lists
            layer1_values = new byte[sizex, sizey];
            layer1_oldvalues = new byte[sizex, sizey];
            layer1_newvalues = new byte[sizex, sizey];
            layer2_values = new byte[sizex, sizey];
            AllreadyChanged = new byte[sizex, sizex];
            generation0_values = new byte[sizex, sizey];
            MayHaveActiveCells = new bool[size_mul16x, size_mul16y];
            HasChangedsincegen0 = new bool[sizex, sizey];
            UpdateTex16x16 = new short[size_mul16x, size_mul16y];
            UpdateTex16x16_onepixpos = new int2[size_mul16x, size_mul16y];
            activeCells = new List<int2>();
            PossibleCellIndexChange = new List<int2>();
            NeedCheck4ActiveState = new List<int2>();
            UpdateTex16x16_List = new List<int2>();

            // Loading and Generating Textures
            logictex = new Texture2D(GraphicsDevice, sizex, sizey, false, SurfaceFormat.Alpha8);
            outputtex = new Texture2D(GraphicsDevice, Screenwidth, Screenheight, false, SurfaceFormat.Color);
        }

        protected override void UnloadContent()
        {
        }

        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KB_currentstate = Keyboard.GetState();
            M_currentstate = Mouse.GetState();
            Vector2 screenpos = new Vector2(M_currentstate.Position.X, M_currentstate.Position.Y);
            int worldposx, worldposy;
            screen2worldcoo_int(screenpos, out worldposx, out worldposy);

            int PORTAL;
            if (IsActive == false)
                goto PORTAL;

            //
            // Position, Zoom and Inputs
            //
            #region Position and Zoom and Inputs

            if (KB_currentstate.IsKeyDown(Keys.W))
                worldpos.Y += 10;
            if (KB_currentstate.IsKeyDown(Keys.S))
                worldpos.Y -= 10;
            if (KB_currentstate.IsKeyDown(Keys.A))
                worldpos.X += 10;
            if (KB_currentstate.IsKeyDown(Keys.D))
                worldpos.X -= 10;

            worldpos.X = (int)(worldpos.X);
            worldpos.Y = (int)(worldpos.Y);

            if (KB_currentstate.IsKeyDown(Keys.Add) && !KB_oldstate.IsKeyDown(Keys.Add))
                simulationspeed -= 1;
            if (KB_currentstate.IsKeyDown(Keys.Subtract) && !KB_oldstate.IsKeyDown(Keys.Subtract))
                simulationspeed += 1;

            // Play and break with Space Bar
            if (KB_currentstate.IsKeyDown(Keys.Space) && !KB_oldstate.IsKeyDown(Keys.Space))
                IsSimulating = !IsSimulating;

            if (M_currentstate.ScrollWheelValue != M_oldstate.ScrollWheelValue)
            {
                if (M_currentstate.ScrollWheelValue < M_oldstate.ScrollWheelValue) // Zooming Out
                {
                    worldzoom -= 1;
                    Vector2 diff = M_currentstate.Position.ToVector2() - worldpos;
                    worldpos += diff / 2;
                }
                else // Zooming In
                {
                    worldzoom += 1;
                    Vector2 diff = M_currentstate.Position.ToVector2() - worldpos;
                    worldpos -= diff;
                }
            }
            #endregion

            //
            // Checking buttons
            //
            #region Checking Buttons

            bool IsPressed = false;
            // Play and break with button
            if (M_currentstate.LeftButton == ButtonState.Pressed && screenpos.X > 20 && screenpos.Y > 20 && screenpos.X < 55 && screenpos.Y < 55)
            {
                IsPressed = true;
                if (M_oldstate.LeftButton == ButtonState.Released) // Clicked the first time on play button
                    IsSimulating = !IsSimulating;

            }

            // Reset Button
            if (M_currentstate.LeftButton == ButtonState.Pressed && screenpos.X > 20 && screenpos.Y > 75 && screenpos.X < 55 && screenpos.Y < 115)
            {
                IsPressed = true;
                if (M_oldstate.LeftButton == ButtonState.Released && currentsimulationstep > 0) // Clicked the first time on reset button and the generation is greater than 0
                {
                    activeCells.Clear();
                    Removeunnecessarygeneration0changes();
                    for (LinkedListNode<int2> it = generation0changed.First; it != null; it = it.Next)
                    {
                        int x = it.Value.x;
                        int y = it.Value.y;
                        Set_CellValues((byte)(generation0_values[x, y] % 17), (byte)(generation0_values[x, y] / 17), x, y);
                        HasChangedsincegen0[x, y] = false;
                        //layer1_values[x, y] = layer1_newvalues[x, y] = (byte)(generation0_values[x, y] % 17);
                        //layer2_values[x, y] = (byte)(generation0_values[x, y] / 17);
                        //PossibleCellIndexChange.Add(it.Value);
                        //Checkfornewactivecells(it.Value);
                    }
                    generation0changed.Clear();
                    currentsimulationstep = 0;
                    newaddedelemntscounter = 0;
                    IsSimulating = false;
                }

            }

            // Layer 1 Buttons
            for (int i = 0; i < layer1_elements.Count; i++)
            {
                if (M_currentstate.Position.X > 80 + 40 * i && M_currentstate.X < 112 + 40 * i && M_currentstate.Y > 20 && M_currentstate.Y < 52 && M_currentstate.LeftButton == ButtonState.Pressed)
                {
                    currentcellindex = (byte)layer1_elements[i].index;
                    IsPressed = true;
                }
            }

            // Layer 2 Buttons
            for (int i = 0; i < layer2_elements.Count; i++)
            {
                if (M_currentstate.Position.X > 80 + 40 * i && M_currentstate.X < 112 + 40 * i && M_currentstate.Y > 60 && M_currentstate.Y < 92 && M_currentstate.LeftButton == ButtonState.Pressed)
                {
                    currentcellindex = (byte)layer2_elements[i].index;
                    IsPressed = true;
                }
            }

            #endregion

            //
            // Selction
            //

            #region Selection

            if (KB_currentstate.IsKeyDown(Keys.Escape))
            {
                currentselectiontype = 0;
                IsSelecting = false;
            }

            if (KB_currentstate.IsKeyDown(Keys.LeftControl) && M_currentstate.LeftButton == ButtonState.Pressed && KB_oldstate.IsKeyDown(Keys.LeftControl) && M_oldstate.LeftButton == ButtonState.Released)
            {
                IsSelecting = true;
                currentselectiontype = 1;
                Selection_Start = new int2(worldposx, worldposy);
            }

            if (currentselectiontype == 1 && KB_currentstate.IsKeyDown(Keys.LeftControl) && M_currentstate.LeftButton == ButtonState.Pressed)
            {
                Selection_End = new int2(worldposx, worldposy);
                Selection_Start_total.x = Selection_Start.x;
                Selection_Start_total.y = Selection_Start.y;
                Selection_End_total.x = Selection_End.x;
                Selection_End_total.y = Selection_End.y;
                if (Selection_End.x < Selection_Start.x)
                {
                    Selection_Start_total.x = Selection_End.x;
                    Selection_End_total.x = Selection_Start.x;
                }
                if (Selection_End.y < Selection_Start.y)
                {
                    Selection_Start_total.y = Selection_End.y;
                    Selection_End_total.y = Selection_Start.y;
                }
            }

            // Deleting Selected Cells
            if (currentselectiontype == 1 && KB_currentstate.IsKeyDown(Keys.Delete) && KB_oldstate.IsKeyUp(Keys.Delete))
            {
                for (int x = Selection_Start_total.x; x <= Selection_End_total.x; ++x)
                {
                    for (int y = Selection_Start_total.y; y <= Selection_End_total.y; ++y)
                    {
                        int oldlayer2value = layer2_values[x, y];
                        Set_CellValues(0, 0, x, y);
                        if (oldlayer2value != 0)
                            SetPixel_1x1(x, y, logictex);
                    }
                }
            }

            #region Copying Selection

            if (currentselectiontype == 1 && KB_currentstate.IsKeyDown(Keys.LeftControl) && KB_currentstate.IsKeyDown(Keys.C) && KB_oldstate.IsKeyDown(Keys.LeftControl) && KB_oldstate.IsKeyUp(Keys.C))
            {
                CopiedCells = new byte[Selection_End_total.x - Selection_Start_total.x + 1, Selection_End_total.y - Selection_Start_total.y + 1];
                for (int x = Selection_Start_total.x; x <= Selection_End_total.x; ++x)
                {
                    for (int y = Selection_Start_total.y; y <= Selection_End_total.y; ++y)
                    {
                        CopiedCells[x - Selection_Start_total.x, y - Selection_Start_total.y] = (byte)(layer2_values[x, y] * 17 + layer1_values[x, y]);
                    }
                }
            }

            #endregion

            #region Spawning, Placing and moving Copy 

            // Spawning Copy
            if (CopiedCells != null && currentselectiontype < 2 && KB_currentstate.IsKeyDown(Keys.LeftControl) && KB_currentstate.IsKeyDown(Keys.V) && KB_oldstate.IsKeyDown(Keys.LeftControl) && KB_oldstate.IsKeyUp(Keys.V))
            {
                currentselectiontype = 2;
                if (CopyTexture != null && CopyTexture.IsDisposed == false)
                    CopyTexture.Dispose();
                CopyTexture = new Texture2D(GraphicsDevice, CopiedCells.GetLength(0), CopiedCells.GetLength(1), false, SurfaceFormat.Alpha8);
                byte[] newData = new byte[CopiedCells.Length];
                for (int x = 0; x < CopiedCells.GetLength(0); ++x)
                {
                    for (int y = 0; y < CopiedCells.GetLength(1); ++y)
                    {
                        newData[x + y * CopiedCells.GetLength(0)] = CopiedCells[x, y];
                    }
                }
                CopyTexture.SetData(newData);
                renderEffect.Parameters["CopyTexture"].SetValue(CopyTexture);
                renderEffect.Parameters["copiedwidth"].SetValue(CopiedCells.GetLength(0));
                renderEffect.Parameters["copiedheight"].SetValue(CopiedCells.GetLength(1));
                copiedpos.x = worldposx;
                copiedpos.y = worldposy;

            }

            // Moving
            if (currentselectiontype == 2 && M_currentstate.LeftButton == ButtonState.Pressed && worldposx >= copiedpos.x && worldposy >= copiedpos.y && worldposx < copiedpos.x + CopiedCells.GetLength(0) && worldposy < copiedpos.y + CopiedCells.GetLength(1))
                IsCopyMoving = true;
            if (IsCopyMoving)
            {
                if (M_currentstate.LeftButton == ButtonState.Released)
                    IsCopyMoving = false;
                else
                {
                    int difx = worldposx - worldposx_old;
                    int dify = worldposy - worldposy_old;
                    copiedpos.x = MathHelper.Clamp(copiedpos.x + difx, 2, sizex - 2 - CopiedCells.GetLength(0));
                    copiedpos.y = MathHelper.Clamp(copiedpos.y + dify, 2, sizey - 2 - CopiedCells.GetLength(1));
                }
            }

            if (currentselectiontype == 2)
            {
                if (KB_currentstate.IsKeyDown(Keys.X) && KB_oldstate.IsKeyUp(Keys.X))
                {
                    int sizex = CopiedCells.GetLength(0);
                    int sizey = CopiedCells.GetLength(1);

                    byte[,] newCopiedCells = new byte[sizex, sizey];
                    for (int x = 0; x < sizex; ++x)
                    {
                        for (int y = 0; y < sizey; ++y)
                        {
                            newCopiedCells[x, y] = CopiedCells[sizex - x - 1, y];
                            byte val = (byte)(newCopiedCells[x, y] / 17);

                            if (val == 2 || val == 6 || val == 10)
                                newCopiedCells[x, y] += 2 * 17;
                            else if (val == 4 || val == 8 || val == 12)
                                newCopiedCells[x, y] -= 2 * 17;
                        }
                    }
                    CopiedCells = newCopiedCells;
                    byte[] newData = new byte[CopiedCells.Length];
                    for (int x = 0; x < sizex; ++x)
                    {
                        for (int y = 0; y < sizey; ++y)
                        {
                            newData[x + y * sizex] = CopiedCells[x, y];
                        }
                    }
                    CopyTexture.SetData(newData);
                }
                if (KB_currentstate.IsKeyDown(Keys.Y) && KB_oldstate.IsKeyUp(Keys.Y))
                {
                    int sizex = CopiedCells.GetLength(0);
                    int sizey = CopiedCells.GetLength(1);

                    byte[,] newCopiedCells = new byte[sizex, sizey];
                    for (int x = 0; x < sizex; ++x)
                    {
                        for (int y = 0; y < sizey; ++y)
                        {
                            newCopiedCells[x, y] = CopiedCells[x, sizey - y - 1];
                            byte val = (byte)(newCopiedCells[x, y] / 17);

                            if (val == 1 || val == 5 || val == 9)
                                newCopiedCells[x, y] += 2 * 17;
                            else if (val == 3 || val == 7 || val == 11)
                                newCopiedCells[x, y] -= 2 * 17;
                        }
                    }
                    CopiedCells = newCopiedCells;
                    byte[] newData = new byte[CopiedCells.Length];
                    for (int x = 0; x < sizex; ++x)
                    {
                        for (int y = 0; y < sizey; ++y)
                        {
                            newData[x + y * sizex] = CopiedCells[x, y];
                        }
                    }
                    CopyTexture.SetData(newData);
                }
                if (KB_currentstate.IsKeyDown(Keys.R) && KB_oldstate.IsKeyUp(Keys.R))
                {
                    int sizex = CopiedCells.GetLength(0);
                    int sizey = CopiedCells.GetLength(1);

                    byte[,] newCopiedCells = new byte[sizey, sizex];
                    for (int x = 0; x < sizey; ++x)
                    {
                        for (int y = 0; y < sizex; ++y)
                        {
                            newCopiedCells[x, y] = CopiedCells[y, sizey - x - 1];
                            byte val = (byte)(newCopiedCells[x, y] / 17);
                            if ((val >= 1 && val < 4) || (val >= 5 && val < 8) || (val >= 9 && val < 12))
                                newCopiedCells[x, y] += 17;
                            else if (val == 4 || val == 8 || val == 12)
                                newCopiedCells[x, y] -= 17 * 3;
                        }
                    }
                    if (CopyTexture != null && CopyTexture.IsDisposed == false)
                        CopyTexture.Dispose();
                    CopyTexture = new Texture2D(GraphicsDevice, sizey, sizex, false, SurfaceFormat.Alpha8);
                    CopiedCells = newCopiedCells;
                    byte[] newData = new byte[CopiedCells.Length];
                    for (int x = 0; x < sizey; ++x)
                    {
                        for (int y = 0; y < sizex; ++y)
                        {
                            newData[x + y * sizey] = CopiedCells[x, y];
                        }
                    }
                    CopyTexture.SetData(newData);
                    renderEffect.Parameters["CopyTexture"].SetValue(CopyTexture);
                    renderEffect.Parameters["copiedwidth"].SetValue(sizey);
                    renderEffect.Parameters["copiedheight"].SetValue(sizex);
                }
            }

            // Placing Copy
            if (currentselectiontype == 2 && KB_currentstate.IsKeyDown(Keys.Enter))
            {
                currentselectiontype = 0;
                int copie_sizex = CopiedCells.GetLength(0);
                int copie_sizey = CopiedCells.GetLength(1);
                for (int x = 0; x < copie_sizex; ++x)
                {
                    for (int y = 0; y < copie_sizey; ++y)
                    {
                        //if (layer2_values[copiedpos.x + x, copiedpos.y + y] * 17 + layer1_values[copiedpos.x + x, copiedpos.y + y] != CopiedCells[x, y])
                        {
                            Set_CellValues((byte)(CopiedCells[x, y] % 17), (byte)(CopiedCells[x, y] / 17), copiedpos.x + x, copiedpos.y + y);
                        }
                    }
                }
            }

            #endregion


            #endregion

            #region SAVING

            if (KB_currentstate.IsKeyDown(Keys.LeftControl) && KB_currentstate.IsKeyDown(Keys.LeftShift) && KB_currentstate.IsKeyDown(Keys.S) && KB_oldstate.IsKeyUp(Keys.S))
            {
                IsSimulating = false;
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    try
                    {
                        string savepath = System.IO.Directory.GetCurrentDirectory() + "\\SAVES";
                        System.IO.Directory.CreateDirectory(savepath);
                        dialog.InitialDirectory = savepath;
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Error while trying to create Save folder: {0}", exp);
                    }
                    dialog.Multiselect = false;
                    dialog.CheckPathExists = false;
                    dialog.CheckFileExists = false;
                    dialog.Title = "Select or Create File to Save";
                    dialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string filename = dialog.FileName;
                        try
                        {
                            FileStream stream = new FileStream(filename, FileMode.Create);
                            List<byte> bytestosave = new List<byte>();
                            uint counter = 0;
                            for (uint x = 0; x < size_mul16x; ++x)
                            {
                                for (uint y = 0; y < size_mul16y; ++y)
                                {
                                    if (MayHaveActiveCells[x, y] == true)
                                    {
                                        byte[] data_16x16 = new byte[16 * 16];
                                        bool IsEmpty = true;
                                        for (uint x2 = 0; x2 < 16; ++x2)
                                        {
                                            for (uint y2 = 0; y2 < 16; ++y2)
                                            {
                                                byte value12 = (byte)(layer2_values[x * 16 + x2, y * 16 + y2] * 17 + layer1_values[x * 16 + x2, y * 16 + y2]);

                                                data_16x16[x2 + y2 * 16] = value12;
                                                if (value12 != 0)
                                                {
                                                    IsEmpty = false;
                                                }
                                            }
                                        }

                                        if (!IsEmpty)
                                        {
                                            bytestosave.AddRange(BitConverter.GetBytes(x));
                                            bytestosave.AddRange(BitConverter.GetBytes(y));
                                            bytestosave.AddRange(data_16x16);
                                            counter++;
                                        }
                                        else
                                            MayHaveActiveCells[x, y] = false;
                                    }
                                }
                            }
                            // Saves Number of chunks
                            stream.Write(BitConverter.GetBytes(size_mul16x), 0, 4);
                            stream.Write(BitConverter.GetBytes(size_mul16y), 0, 4);
                            stream.Write(BitConverter.GetBytes(counter), 0, 4);
                            stream.Write(bytestosave.ToArray(), 0, bytestosave.Count);
                            stream.Close();
                            stream.Dispose();
                            Console.WriteLine("Saving suceeded. Filename: {0}", filename);

                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Saving failed: {0}", exp);
                            System.Windows.Forms.MessageBox.Show("Saving failed", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            #endregion

            #region LOADING

            if (KB_currentstate.IsKeyDown(Keys.LeftControl) && KB_currentstate.IsKeyDown(Keys.LeftShift) && KB_currentstate.IsKeyDown(Keys.O) && KB_oldstate.IsKeyUp(Keys.O))
            {
                IsSimulating = false;
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    try
                    {
                        string savepath = System.IO.Directory.GetCurrentDirectory() + "\\SAVES";
                        System.IO.Directory.CreateDirectory(savepath);
                        dialog.InitialDirectory = savepath;
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Error while trying to create Save folder: {0}", exp);
                    }

                    dialog.Multiselect = false;
                    dialog.CheckFileExists = true;
                    dialog.CheckPathExists = true;
                    dialog.Title = "Select File to Open";
                    dialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string filename = dialog.FileName;
                        try
                        {
                            FileStream stream = new FileStream(filename, FileMode.Open);

                            layer1_values = new byte[sizex, sizey];
                            layer1_oldvalues = new byte[sizex, sizey];
                            layer1_newvalues = new byte[sizex, sizey];
                            layer2_values = new byte[sizex, sizey];
                            AllreadyChanged = new byte[sizex, sizex];
                            generation0_values = new byte[sizex, sizey];
                            MayHaveActiveCells = new bool[size_mul16x, size_mul16y];
                            UpdateTex16x16 = new short[size_mul16x, size_mul16y];
                            UpdateTex16x16_onepixpos = new int2[size_mul16x, size_mul16y];
                            activeCells.Clear();
                            PossibleCellIndexChange.Clear();
                            NeedCheck4ActiveState.Clear();
                            UpdateTex16x16_List.Clear();
                            logictex.Dispose();
                            logictex = new Texture2D(GraphicsDevice, sizex, sizey, false, SurfaceFormat.Alpha8);
                            outputtex.Dispose();
                            outputtex = new Texture2D(GraphicsDevice, Screenwidth, Screenheight, false, SurfaceFormat.Color);

                            byte[] intbuffer = new byte[4];

                            stream.Read(intbuffer, 0, 4);
                            size_mul16x = BitConverter.ToInt32(intbuffer, 0);
                            stream.Read(intbuffer, 0, 4);
                            size_mul16y = BitConverter.ToInt32(intbuffer, 0);
                            sizex = size_mul16x * 16;
                            sizey = size_mul16y * 16;

                            stream.Read(intbuffer, 0, 4);
                            int count = BitConverter.ToInt32(intbuffer, 0);

                            byte[] chunkdata = new byte[16 * 16];
                            for (int i = 0; i < count; ++i)
                            {
                                // Reading X and Y pos of chunks to load
                                stream.Read(intbuffer, 0, 4);
                                int xpos = BitConverter.ToInt32(intbuffer, 0);
                                stream.Read(intbuffer, 0, 4);
                                int ypos = BitConverter.ToInt32(intbuffer, 0);

                                // Reading chunk data
                                stream.Read(chunkdata, 0, 16 * 16);
                                for (int x = 0; x < 16; ++x)
                                {
                                    for (int y = 0; y < 16; ++y)
                                    {
                                        Set_CellValues((byte)(chunkdata[x + y * 16] % 17), (byte)(chunkdata[x + y * 16] / 17), xpos * 16 + x, ypos * 16 + y);
                                    }
                                }

                            }

                            stream.Close();
                            stream.Dispose();
                            Console.WriteLine("Loading suceeded. Filename: {0}", filename);

                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Loading failed: {0}", exp);
                            System.Windows.Forms.MessageBox.Show("Loading failed: " + exp, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            #endregion

            // Placing and Removing Cells
            if (IsPressed == false && IsSelecting == false && currentselectiontype == 0)
            {
                if (worldposx > 1 && worldposy > 1 && worldposx < sizex - 2 && worldposy < sizey - 2 && M_currentstate.LeftButton == ButtonState.Pressed)
                {
                    if (currentcellindex < 14)
                    {
                        if ((currentcellindex < 5 && layer2_values[worldposx, worldposy] != 0) || layer2_values[worldposx, worldposy] == 0)
                            Set_CellValues(currentcellindex, layer2_values[worldposx, worldposy], worldposx, worldposy);
                        else
                            Set_CellValues(currentcellindex, 0, worldposx, worldposy);
                    }
                    else if (layer1_values[worldposx, worldposy] < 5)
                        Set_CellValues(layer1_values[worldposx, worldposy], (byte)(currentcellindex - 13), worldposx, worldposy);
                    else
                        Set_CellValues(0, (byte)(currentcellindex - 13), worldposx, worldposy);
                }
                else if (worldposx > 1 && worldposy > 1 && worldposx < sizex - 2 && worldposy < sizey - 2 && M_currentstate.RightButton == ButtonState.Pressed)
                {
                    Set_CellValues(0, 0, worldposx, worldposy);
                    //SetGraphicsDevice_CellValues(layer1_values[worldposx, worldposy], layer2_values[worldposx, worldposy], worldposx, worldposy);
                }
            }

            //Checking for new active Cells
            if (NeedCheck4ActiveState.Count > 0)
            {
                for (int i = 0; i < NeedCheck4ActiveState.Count; ++i)
                {
                    Checkfornewactivecells(NeedCheck4ActiveState[i]);
                }
                NeedCheck4ActiveState.Clear();
            }

            //
            // S I M U L A T I O N
            //
            if (IsSimulating)
            {
                if (simulationspeed < 0)
                {
                    UpdateActiveCells(); // Checks for out of bound active -+cells
                    for (int i = 0; i < Math.Pow(2, -simulationspeed); ++i)
                    {
                        _count = i;
                        Stopwatch watch2 = new Stopwatch();
                        watch2.Start();
                        watch2.Stop();
                        simulator.UpdateOneStep();
                        currentsimulationstep++;
                    }
                }
                else
                {
                    _count = 0;
                    simulationtimer++;
                    if (simulationtimer >= Math.Pow(2, simulationspeed))
                    {
                        simulationtimer = 0;
                        UpdateActiveCells(); // Checks for out of bound active cells
                        simulator.UpdateOneStep();
                        currentsimulationstep++;
                    }
                }
            }
            Stopwatch watch = new Stopwatch();
            watch.Start();
            // Checking for changes of all cells to update the logic texture
            setanz = 0;
            for (int i = 0; i < PossibleCellIndexChange.Count; ++i)
            {
                int x = PossibleCellIndexChange[i].x;
                int y = PossibleCellIndexChange[i].y;

                // Cell type changed
                if (layer1_values[x, y] != layer1_oldvalues[x, y] || layer2_values[x, y] != 0)
                {
                    layer1_oldvalues[x, y] = layer1_values[x, y];
                    int xx = x / 16;
                    int yy = y / 16;
                    if (++UpdateTex16x16[xx, yy] < 2)
                    {
                        UpdateTex16x16_onepixpos[xx, yy] = new int2(x, y);
                        UpdateTex16x16_List.Add(new int2(xx, yy));
                    }
                }

                if (currentsimulationstep == 0)
                    generation0_values[x, y] = (byte)(layer2_values[x, y] * 17 + layer1_values[x, y]);// Set Generation 0 values
                else if (generation0_values[x, y] != layer2_values[x, y] * 17 + layer1_values[x, y] && HasChangedsincegen0[x, y] == false) // save changes for reseting
                {
                    newaddedelemntscounter++;
                    generation0changed.AddLast(new int2(x, y));
                    HasChangedsincegen0[x, y] = true;
                }

            }
            PossibleCellIndexChange.Clear();
            for (int i = 0; i < UpdateTex16x16_List.Count; ++i)
            {
                int x = UpdateTex16x16_List[i].x;
                int y = UpdateTex16x16_List[i].y;

                if (UpdateTex16x16[x, y] == 1)
                    SetPixel_1x1(UpdateTex16x16_onepixpos[x, y].x, UpdateTex16x16_onepixpos[x, y].y, logictex);
                else
                    SetPixel_16x16(x, y, logictex);
                UpdateTex16x16[x, y] = 0;
            }
            UpdateTex16x16_List.Clear();
            if (newaddedelemntscounter > 10000) // Remove unnecessary generationchanges
            {
                Removeunnecessarygeneration0changes();
                newaddedelemntscounter = 0;
            }
            watch.Stop();
            time1 = watch.ElapsedTicks / (float)TimeSpan.TicksPerMillisecond;

            renderEffect.Parameters["zoom"].SetValue((float)Math.Pow(2, worldzoom));
            renderEffect.Parameters["coos"].SetValue(worldpos);
            renderEffect.Parameters["mousepos_X"].SetValue(worldposx);
            renderEffect.Parameters["mousepos_Y"].SetValue(worldposy);
            renderEffect.Parameters["currentselectiontype"].SetValue(currentselectiontype);
            renderEffect.Parameters["Selection_StartX"].SetValue(Selection_Start_total.x);
            renderEffect.Parameters["Selection_EndX"].SetValue(Selection_End_total.x);
            renderEffect.Parameters["Selection_StartY"].SetValue(Selection_Start_total.y);
            renderEffect.Parameters["Selection_EndY"].SetValue(Selection_End_total.y);
            if (currentselectiontype == 2)
            {
                renderEffect.Parameters["copiedposx"].SetValue(copiedpos.x);
                renderEffect.Parameters["copiedposy"].SetValue(copiedpos.y);
            }


        PORTAL:
            KB_oldstate = KB_currentstate;
            M_oldstate = M_currentstate;
            worldposx_old = worldposx;
            worldposy_old = worldposy;
            base.Update(gameTime);
        }

        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Vector2 screenpos = new Vector2(M_currentstate.Position.X, M_currentstate.Position.Y);
            int worldposx, worldposy;
            screen2worldcoo_int(screenpos, out worldposx, out worldposy);

            GraphicsDevice.Clear(Color.Black);
            renderEffect.Parameters["logictex"].SetValue(logictex);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, null, null, renderEffect, Matrix.Identity);
            spriteBatch.Draw(outputtex, Vector2.Zero, Color.White);
            spriteBatch.End();

            // Drawing Overlays
            spriteBatch.Begin();
            if (worldzoom >= 3)
            {
                int topleftx, toplefty, bottomrightx, bottomrighty;
                screen2worldcoo_int(Vector2.Zero, out topleftx, out toplefty);
                screen2worldcoo_int(new Vector2(Screenwidth, Screenheight), out bottomrightx, out bottomrighty);
                Vector2 worldpos_float = screen2worldcoo_Vector2(Vector2.Zero);
                float worldkommax = worldpos_float.X % 1.0f;
                float worldkommay = worldpos_float.Y % 1.0f;
                for (int x = topleftx; x < bottomrightx + 1; ++x)
                {
                    for (int y = toplefty; y < bottomrighty + 1; ++y)
                    {
                        if (x >= 0 && x < sizex && y >= 0 && y < sizey && layer2_values[x, y] > 0)
                        {
                            float pow = (float)Math.Pow(2, worldzoom);
                            spriteBatch.Draw(overlays[layer2_values[x, y] - 1].tex, new Vector2(((x - topleftx) - worldkommax) * pow, ((y - toplefty) - worldkommay) * pow), new Rectangle(0, 0, 64, 64), Color.White, 0, Vector2.Zero, pow / 64.0f, SpriteEffects.None, 0);
                        }
                    }
                }
            }

            spriteBatch.End();

            spriteBatch.Begin();
            for (int i = 0; i < layer1_elements.Count; i++)
            {
                spriteBatch.Draw(layer1_elements[i].tex, new Vector2(80 + i * 40, 20), Color.White);
            }
            for (int i = 0; i < layer2_elements.Count; i++)
            {
                spriteBatch.Draw(layer2_elements[i].tex, new Vector2(80 + i * 40, 60), Color.White);
            }
            if (IsSimulating == false)
                spriteBatch.Draw(button_play, new Vector2(20, 20), Color.White);
            else
                spriteBatch.Draw(button_break, new Vector2(20, 20), Color.White);
            if (currentsimulationstep > 0)
                spriteBatch.Draw(button_reset, new Vector2(20, 75), Color.White);
            else
                spriteBatch.Draw(button_reset, new Vector2(20, 75), Color.Gray);

            spriteBatch.DrawString(font, "Worldzoom: 2^" + worldzoom.ToString() + " = " + ((float)Math.Pow(2, worldzoom)).ToString(), new Vector2(20, 190), Color.Red);
            spriteBatch.DrawString(font, "Speed: 2^" + simulationspeed.ToString(), new Vector2(20, 210), Color.Red);
            spriteBatch.DrawString(font, "Current Generation: " + currentsimulationstep.ToString(), new Vector2(20, 230), Color.Red);
            if (worldposx >= 0 && worldposx < sizex && worldposy >= 0 && worldposy < sizey)
            {
                spriteBatch.DrawString(font, "Worldpos: " + (worldpos).ToString(), new Vector2(20, 250), Color.Red);
                spriteBatch.DrawString(font, "Type_Layer1: " + ((int)(layer1_values[worldposx, worldposy])).ToString(), new Vector2(20, 270), Color.Red);
                spriteBatch.DrawString(font, "Type_Layer2: " + ((int)(layer2_values[worldposx, worldposy])).ToString(), new Vector2(20, 290), Color.Red);
                bool isactive = false;
                for (int i = 0; i < activeCells.Count; ++i)
                {
                    if (activeCells[i].x == worldposx && activeCells[i].y == worldposy)
                        isactive = true;
                }
                spriteBatch.DrawString(font, "ActiveState: " + isactive.ToString(), new Vector2(20, 310), Color.Red);
                spriteBatch.DrawString(font, "count: " + newaddedelemntscounter.ToString(), new Vector2(20, 330), Color.Red);
                spriteBatch.DrawString(font, "selectiontype: " + currentselectiontype.ToString(), new Vector2(20, 350), Color.Red);
                spriteBatch.DrawString(font, "TIME1: " + setanz.ToString(), new Vector2(20, 370), Color.Red);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}





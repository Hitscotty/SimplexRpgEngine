﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Serialization;
using DarkUI.Collections;
using DarkUI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using MonoGame.Extended.Shapes;
using MonoGame.Forms.Controls;
using Newtonsoft.Json;
using SimplexCore;
using SimplexCore.Ext;
using SimplexResources.Objects;
using SimplexResources.Rooms;
using Bitmap = System.Drawing.Bitmap;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Color = Microsoft.Xna.Framework.Color;
using FillMode = Microsoft.Xna.Framework.Graphics.FillMode;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = SharpDX.Point;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = MonoGame.Extended.RectangleF;

namespace SimplexIde
{
    public class DrawTest : UpdateWindow
    {
        public Type SelectedObject = null;
        public List<GameObject> SceneObjects = new List<GameObject>();
        public List<TextureReference> Textures = new List<TextureReference>();
        public bool DrawGrid;
        GameTime g = new GameTime();
        public static Color BackgroundColor = Color.CornflowerBlue;
        public List<Spritesheet> Sprites = new List<Spritesheet>();
        Camera2D camera;
        SimplexCamera cam = new SimplexCamera();
        KeyboardState prevState;
        public Vector2 MousePosition;
        public Vector2 MousePositionTranslated;
        public GameObject clickedObject = null;
        public List<GameObject> selectedRectangleObjects = new List<GameObject>();
        private bool mouseLocked = false;
        private Vector2 helpVec;
        private Vector2 clickedVec;
        private bool panView = false;
        public DarkContextMenu cms;
        private bool killClick = false;
        private bool cmsOpen = false;
        private bool goodBoy = false;
        Stack<List<GameObject>> stackedSteps = new Stack<List<GameObject>>(32);
        public static DynamicVertexBuffer vertexBuffer;
        public static BasicEffect basicEffect;
        private float k = 0;
        private VertexPositionColor[] _vertexPositionColors;
        Matrix world = Matrix.Identity;
        private Matrix m;
        private SimplexCore.Ext.MgPrimitiveBatcher mpb;
        public GameRoom currentRoom;
        public LayerTool lt;
        public RoomLayer selectedLayer;
        public List<RoomLayer> roomLayers = new List<RoomLayer>();
        public RectangleF selectionRectangle = new RectangleF();

        public Vector2 GridSize = new Vector2(32, 32);
        public Vector2 GridSizeRender = new Vector2(32, 32);
        public Form1 editorForm;
        public bool GameRunning = true;
        public Vector2 MousePrevious = new Vector2();
        public AutotileDefinition currentAutotile = null;
        public TileLayer currentTileLayer = null;
        public RoomLayer lastLayer = null;
        public Keys lastKey = Keys.None;
        public ToolStripItemCollection toolStripItems = new ToolStripItemCollection(new ToolStrip(), new ToolStripItem[] {});

        SpatialHash sh = new SpatialHash() {CellSize = 128, Cols = 20, Rows = 20};
        private GlobalKeyboardHook _globalKeyboardHook;
        public List<Tileset> tilesets;
        public RoomsControl roomsControl;

        protected override void Initialize()
        {
            base.Initialize();
               
            Sgml.SceneObjects = SceneObjects;
            Sgml.roomLayers = roomLayers;
            Sgml.Textures = Textures;
            Sgml.RoomEditor = this;
            Sgml.RoomEditorEditor = Editor;

            camera = new Camera2D(Editor.graphics);
            basicEffect = new BasicEffect(Editor.graphics);

            cam.Camera = camera;
            cam.Position = Vector2.Zero;
            cam.TargetPosition = Vector2.Zero;
            cam.TransformSpeed = 0.1f;

            prevState = Keyboard.GetState();
            basicEffect = new BasicEffect(GraphicsDevice);

            vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 1000, BufferUsage.WriteOnly);
            m = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, -1);
            mpb = new MgPrimitiveBatcher(Editor.graphics, Editor.Font);
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;

            Sprites = JsonConvert.DeserializeObject<List<Spritesheet>>(new StreamReader("../../../SimplexRpgEngine3/SpritesDescriptor.json").ReadToEnd());

            foreach (Spritesheet s in Sprites)
            {
                Texture2D tex = Editor.Content.Load<Texture2D>(Path.GetFullPath("../../../SimplexRpgEngine3/Content/bin/Windows/Sprites/" + s.Name));
                s.Texture = tex;
            }

            Sgml.sh = sh;
            Sgml.Sprites = Sprites;
            Sgml.GraphicsDevice = GraphicsDevice;

            tilesets = JsonConvert.DeserializeObject<List<Tileset>>(File.ReadAllText("../../../SimplexRpgEngine3/TilesetsDescriptor.json"));
            Sgml.tilesets = tilesets;
        }

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            Input.PressDirect(e.KeyboardData.VirtualCode);
        }

        public void RenderLayers(TreeView tv)
        {
            tv.Nodes[0].Nodes.Clear();

            if (currentRoom != null)
            {
                GameRoom gr = (GameRoom)Activator.CreateInstance(currentRoom.GetType());

                foreach (RoomLayer rl in gr.Layers)
                {
                    tv.Nodes[0].Nodes.Add(new TreeNode(rl.Name));
                }
            }
        }

        public void Rsize()
        {
            Editor.graphics.Viewport = new Viewport(0, 0, this.Width, this.Height);         
            m = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, -1);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState ks = new KeyboardState();
            Keys[] keys = ks.GetPressedKeys();

            if (ks.IsKeyUp(lastKey))
            {
                lastKey = Keys.None;
            }
            
            if (keys.Length > 0 && lastKey == Keys.None)
            {
                lastKey = keys[0];
                var keyValue = keys[0].ToString();
                Sgml.keyboard_string += keyValue;
            }

            MousePositionTranslated = cam.Camera.ScreenToWorld(MousePosition);
            GridSizeRender = new Vector2(SimplexMath.Lerp(GridSizeRender.X, GridSize.X, 0.2f), SimplexMath.Lerp(GridSizeRender.Y, GridSize.Y, 0.2f));

            Input.KeyboardState = ks;

            g = gameTime;
            base.Update(gameTime);

            cam.UpdatePosition();

            foreach (RoomLayer rl in roomLayers)
            {
                if (rl.Visible)
                {
                    if (rl is ObjectLayer)
                    {
                        foreach (GameObject o in ((ObjectLayer)rl).Objects)
                        {
                            if (o.Position.X != o.PositionPrevious.X || o.Position.Y != o.PositionPrevious.Y)
                            {
                                sh.UnregisterObject(o);
                                sh.RegisterObject(o);
                            }

                            if (GameRunning || o == clickedObject)
                            {
                                o.EvtStep();
                            }
                        }
                    }
                }
            }
        }

        public void ToggleGrid(bool toggle)
        {
            DrawGrid = toggle;
        }

        public void ClickRelease(MouseButtons mb)
        {
            mouseLocked = false;

            if ((mb & MouseButtons.Left) != 0) { Input.ReleasedButtons[0] = 1; Input.PressedButtonsOnce[0] = 0; }
            if ((mb & MouseButtons.Right) != 0) { Input.ReleasedButtons[1] = 1; }
            if ((mb & MouseButtons.Middle) != 0) { Input.ReleasedButtons[2] = 1; }
            if ((mb & MouseButtons.None) != 0) { Input.ReleasedButtons[3] = 1; }
            if ((mb & MouseButtons.XButton1) != 0) { Input.ReleasedButtons[5] = 1; }
            if ((mb & MouseButtons.XButton2) != 0) { Input.ReleasedButtons[6] = 1; }

            Input.CheckAnyReleased();


            if (Input.KeyboardState.IsKeyDown(Keys.LeftControl))
            {
                selectedRectangleObjects.Clear();

                foreach (GameObject o in SceneObjects)
                {
                    RectangleF r = new RectangleF(o.Position, new Size2(o.Sprite.ImageRectangle.Width, o.Sprite.ImageRectangle.Height));

                    if (r.Intersects(selectionRectangle))
                    {
                        o.TempPosition = new Vector2(o.Position.X - MousePositionTranslated.X, o.Position.Y - MousePositionTranslated.Y);
                        selectedRectangleObjects.Add(o);
                    }
                }
            }

            selectionRectangle = RectangleF.Empty;

            if (!cms.Visible)
            {
                clickedObject = null;

            }

            panView = false;
        }

        public void InitializeNodes(ObservableList<DarkTreeNode> nodes)
        {
            Sprites = JsonConvert.DeserializeObject<List<Spritesheet>>(new StreamReader("../../../SimplexRpgEngine3/SpritesDescriptor.json").ReadToEnd());

            foreach (Spritesheet s in Sprites)
            {
                Texture2D tex = Editor.Content.Load<Texture2D>(Path.GetFullPath("../../../SimplexRpgEngine3/Content/bin/Windows/Sprites/" + s.Name));
                s.Texture = tex;
            }
        }

        protected override void Draw()
        {
            double framerate = Editor.GetFrameRate;
            KeyboardState ks = Keyboard.GetState();

            base.Draw();
            Matrix transformMatrix = cam.Camera.GetViewMatrix();
            BackgroundColor = Color.Black;
            Editor.graphics.Clear(BackgroundColor);
            Input.MousePosition = MousePositionTranslated;

            Sgml.currentRoom = currentRoom;

            if (DrawGrid)
            {
                Color c = Color.Black;
                c.A = 128;
                Editor.spriteBatch.Begin(transformMatrix: transformMatrix);
                for (float i = 0; i < 768; i += GridSizeRender.Y)
                {
                    for (float j = 0; j < 1024; j += GridSizeRender.X)
                    {
                        i = (float) Math.Round(i);
                        j = (float) Math.Round(j);
                        Editor.spriteBatch.DrawRectangle(new RectangleF(j, i, GridSizeRender.X, GridSizeRender.Y), c, 1);
                    }
                }

                Editor.spriteBatch.End();
            }

            foreach (RoomLayer rl in roomLayers)
            {
                if (rl.LayerType == RoomLayer.LayerTypes.typeTile)
                {
                    Color c = Color.White;
                    c.A = 128;
                    Editor.spriteBatch.Begin(transformMatrix: transformMatrix);
                    int xIndex = 0;
                    int yIndex = 0;
                    for (float i = 0; i < 768; i += GridSizeRender.Y)
                    {
                        for (float j = 0; j < 1024; j += GridSizeRender.X)
                        {
                            xIndex++;
                            i = (float) Math.Round(i);
                            j = (float) Math.Round(j);
                        }

                        yIndex++;
                        xIndex = 0;
                    }

                    foreach (Tile t in ((TileLayer) rl).Tiles)
                    {
                        Editor.spriteBatch.Draw(t.SourceTexture, new Vector2(t.PosX * 32, t.PosY * 32), t.DrawRectangle, Color.White);
                    }

                    Editor.spriteBatch.End();
                }
            }

            Matrix view = cam.Camera.GetViewMatrix();
            Matrix projection = m;

            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
            basicEffect.VertexColorEnabled = true;

            foreach (RoomLayer rl in roomLayers.ToList())
            {
                if (rl.Visible)
                {
                    // ------------ positions doesn't matter here -------------
                    // Layer is tile
                    if (rl is TileLayer)
                    {
                        Editor.spriteBatch.Begin(transformMatrix: transformMatrix);
                        foreach (Tile t in ((TileLayer) rl).Tiles)
                        {
                            Editor.spriteBatch.Draw(t.SourceTexture, new Vector2(t.PosX * 32, t.PosY * 32), t.DrawRectangle, Color.White);
                        }

                        Editor.spriteBatch.End();
                    }

                    // Layer is object
                    if (rl is ObjectLayer)
                    { 
                       foreach (GameObject o in ((ObjectLayer) rl).Objects.ToList())
                       {
                               List<GameObject> possibleColliders = sh.ObjectsNearby(o); // already works for bruteforce

                              /*  foreach (GameObject g2 in possibleColliders)
                                {
                                    if (g2 == o)
                                    {
                                        continue;
                                    }

                                    if (g2.GetType() == typeof(Object3))
                                    {
                                        if (ColliderCircle.CircleInCircle((ColliderCircle)g2.Colliders[0], (ColliderCircle)o.Colliders[0]))
                                        {
                                            if (g2 != o && (o.Colliders[0] as ColliderCircle).Position.X != 0 && (g2.Colliders[0] as ColliderCircle).Position.X != 0) 
                                            {
                                                ((Object3) g2).color = Color.Red;
                                             //   ColliderCircle.ResolveCircleCircleCollisionElastic(g2, o);
                                            }
                                        }
                                        else
                                        {
                                            //((Object3)g2).color = Color.White;
                                        }
                                    }*/
                                
                            
                            o.PositionPrevious = o.Position;
                            o.EvtDraw(Editor.spriteBatch, Editor.Font, o.Sprite.Texture, vertexBuffer, basicEffect,
                                transformMatrix);


                            RectangleF r = new RectangleF(o.Position,
                                new Size2(o.Sprite.ImageRectangle.Width, o.Sprite.ImageRectangle.Height));

                            if (o == clickedObject || r.Intersects(selectionRectangle) ||
                                selectedRectangleObjects.Contains(o))
                            {

                                Editor.spriteBatch.Begin(transformMatrix: transformMatrix);
                                Editor.spriteBatch.DrawRectangle(
                                    new RectangleF(o.Position,
                                        new Size2(o.Sprite.ImageRectangle.Width, o.Sprite.ImageRectangle.Height)),
                                    Color.White,
                                    2);
                                Editor.spriteBatch.End();
                            }
                        }
                    }
                }
            }

                if (ks.IsKeyDown(Keys.LeftControl))
                {
                    Editor.spriteBatch.Begin(transformMatrix: transformMatrix);
                    Editor.spriteBatch.DrawRectangle(selectionRectangle, Color.White, 2);
                    Editor.spriteBatch.End();
                }

                Editor.spriteBatch.Begin();
                Editor.spriteBatch.DrawString(Editor.Font, framerate.ToString("F1"), new Vector2(10, 10), Color.White);
                Editor.spriteBatch.End();


                killClick = false;

                Input.KeyboardStatePrevious = Keyboard.GetState();
                Input.Clear();
        }

        public void PreCheckMouse(MouseEventArgs e)
        {

        }

        public void WheelDown()
        {
            cam.TargetZoom -= 0.1f;
            Input.WheelDown = true;
        }

        public void WheelUp()
        {
            cam.TargetZoom += 0.1f;
            Input.WheelUp = true;
        }

        public void Undo()
        {
            if (stackedSteps.Count > 0)
            {
                SceneObjects = stackedSteps.Pop();
                editorForm.updateStack(stackedSteps.Count);
            }
        }

        public static Texture2D ConvertToTexture(System.Drawing.Bitmap b, GraphicsDevice graphicsDevice)
        {
            Texture2D tx = null;
            using (MemoryStream s = new MemoryStream())
            {
                b.Save(s, System.Drawing.Imaging.ImageFormat.Png);
                s.Seek(0, SeekOrigin.Begin);
                tx = Texture2D.FromStream(graphicsDevice, s);
            }
            return tx;
        }

        public void DeleteAll()
        {
            sh.Clear();

            foreach (RoomLayer rl in roomLayers)
            {
                if (rl is ObjectLayer)
                {
                    ObjectLayer ol = rl as ObjectLayer;
                    ol.Objects.Clear();                
                }
            } 
        }

        public void ClickLock(MouseButtons btn)
        {
            mouseLocked = true;
            MousePrevious = MousePositionTranslated;

            if ((btn & MouseButtons.Left) != 0) { Input.PressedButtonsOnce[0] = 1; Sgml.LastButton = Sgml.MouseButtons.Left;}
            if ((btn & MouseButtons.Right) != 0) { Input.PressedButtonsOnce[1] = 1; Sgml.LastButton = Sgml.MouseButtons.mb_right; }
            if ((btn & MouseButtons.Middle) != 0) { Input.PressedButtonsOnce[2] = 1; Sgml.LastButton = Sgml.MouseButtons.Middle; }
            if ((btn & MouseButtons.None) != 0) { Input.PressedButtonsOnce[3] = 1;}
            if ((btn & MouseButtons.XButton1) != 0) { Input.PressedButtonsOnce[5] = 1; Sgml.LastButton = Sgml.MouseButtons.mb_x1; }
            if ((btn & MouseButtons.XButton2) != 0) { Input.PressedButtonsOnce[6] = 1; Sgml.LastButton = Sgml.MouseButtons.mb_x2; }

            Input.CheckAnyPressedOnce();

            if (selectedRectangleObjects.Count > 0)
            {
                bool flag = false;
                foreach (GameObject o in selectedRectangleObjects)
                {
                    RectangleF r = new RectangleF(o.Position,
                        new Size2(o.Sprite.ImageRectangle.Width, o.Sprite.ImageRectangle.Height));
                    if (r.Intersects(new RectangleF(MousePositionTranslated.X, MousePositionTranslated.Y, 4, 4)))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    selectedRectangleObjects.Clear();
                }
            }

            if (btn == MouseButtons.Left && Input.KeyboardState.IsKeyDown(Keys.LeftControl))
            {
                Vector2 vec = MousePositionTranslated;
                selectionRectangle.X = vec.X;
                selectionRectangle.Y = vec.Y;
            }

            if (btn == MouseButtons.Middle)
            {
                panView = true;
                helpVec = cam.Camera.ScreenToWorld(MousePosition);
            }

            if (btn == MouseButtons.Right && !Input.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                Vector2 vec = MousePositionTranslated;
                if (DrawGrid)
                {
                    vec = new Vector2((int)vec.X / 32 * 32, (int)vec.Y / 32 * 32);
                }

                for (var i = SceneObjects.Count - 1; i >= 0; i--)
                {
                    Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle((int)SceneObjects[i].Position.X, (int)SceneObjects[i].Position.Y, SceneObjects[i].Sprite.ImageRectangle.Width, SceneObjects[i].Sprite.ImageRectangle.Height);

                    if (r.Contains(vec))
                    {
                        if (goodBoy)
                        {
                            cms.Items.Clear();

                            foreach (string items in SceneObjects[i].EditorOptions)
                            {
                                if (items == "[magic_separator]")
                                {
                                    cms.Items.Add(new ToolStripSeparator());
                                }
                                else
                                {
                                    cms.Items.Add(items);
                                }
                            }

                            cms.Invalidate();
                            cms.Size = GetPreferredSize(ClientSize);
                            cms.Show(Cursor.Position);

                            clickedObject = SceneObjects[i];

                            goodBoy = false;

                        }
                        else
                        {
                            goodBoy = true;
                        }

                        break;
                    }
                }
            }
        }


        public void GameClicked(MouseEventArgs e, MouseButtons mb)
        {
            KeyboardState ks = Keyboard.GetState();

            if ((mb & MouseButtons.Left) != 0) { Input.PressedButtonsOnce[0] = 1; Sgml.LastButton = Sgml.MouseButtons.Left; }
            if ((mb & MouseButtons.Right) != 0) { Input.PressedButtonsOnce[1] = 1; Sgml.LastButton = Sgml.MouseButtons.mb_right; }
            if ((mb & MouseButtons.Middle) != 0) { Input.PressedButtonsOnce[2] = 1; Sgml.LastButton = Sgml.MouseButtons.Middle; }
            if ((mb & MouseButtons.None) != 0) { Input.PressedButtonsOnce[3] = 1; }
            if ((mb & MouseButtons.XButton1) != 0) { Input.PressedButtonsOnce[5] = 1; Sgml.LastButton = Sgml.MouseButtons.mb_x1; }
            if ((mb & MouseButtons.XButton2) != 0) { Input.PressedButtonsOnce[6] = 1; Sgml.LastButton = Sgml.MouseButtons.mb_x2; }


            Input.CheckAnyPressed();

            MousePositionTranslated = cam.Camera.ScreenToWorld(MousePosition);

            if (mb == MouseButtons.Left)
            {
                goodBoy = true;
                Vector2 vec = MousePositionTranslated;


                if (DrawGrid)
                {
                    vec = new Vector2((int)vec.X / 32 * 32, (int)vec.Y / 32 * 32);
                }

                if (ks.IsKeyDown(Keys.LeftControl))
                {
                    selectionRectangle.Width = -selectionRectangle.X + vec.X;
                    selectionRectangle.Height = -selectionRectangle.Y + vec.Y;
                }
                else if (clickedObject == null)
                {
                    if (currentAutotile == null)
                    {
                        if (selectedRectangleObjects.Count > 0)
                        {
                            bool flag = true;

                            if (flag)
                            {
                                if (selectedRectangleObjects.Count > 0)
                                {
                                    foreach (GameObject o in selectedRectangleObjects)
                                    {
                                        o.Position += new Vector2(vec.X - MousePrevious.X, vec.Y - MousePrevious.Y);
                                    }
                                }
                            }
                            else
                            {
                                selectedRectangleObjects.Clear();

                            }
                        }
                        else
                        {
                            if (!ks.IsKeyDown(Keys.LeftShift) || Sgml.PlaceEmpty(vec))
                            {
                                if (Sgml.PlaceEmpty(vec))
                                {
                                    if (SelectedObject != null && selectedLayer.GetType() == typeof(ObjectLayer))
                                    {
                                        GameObject o = (GameObject)Activator.CreateInstance(SelectedObject);

                                        Spritesheet s = new Spritesheet();
                                        if (o.Sprite != null)
                                        {
                                            s = Sprites.FirstOrDefault(x => x.Name == o.Sprite.TextureSource);
                                        }

                                        if (!cmsOpen && SelectedObject != null)
                                        {
                                            if (stackedSteps.Count > 31)
                                            {
                                                stackedSteps.Pop();
                                            }

                                            stackedSteps.Push(SceneObjects.ToList());
                                            editorForm.updateStack(stackedSteps.Count);

                                            o.OriginalType = SelectedObject;
                                            o.TypeString = SelectedObject.ToString();

                                            if (s == null)
                                            {
                                                Texture2D tx = ConvertToTexture(Properties.Resources.Question_16x,
                                                    GraphicsDevice);


                                                o.Sprite = new Sprite();
                                                o.Sprite.Texture = tx;
                                                o.Sprite.ImageRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16);
                                            }
                                            else
                                            {
                                                o.Sprite.Texture = s.Texture;
                                                o.Sprite.ImageRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, s.CellWidth, s.CellHeight);
                                            }

                                            o.Sprite.TextureRows = s.Rows;
                                            o.Sprite.TextureCellsPerRow = s.Texture.Width / s.CellWidth;
                                            o.Sprite.ImageSize = new Vector2(s.CellWidth, s.CellHeight);
                                            o.Sprite.FramesCount = Math.Max((s.Texture.Width / s.CellWidth) * (s.Texture.Height / s.CellHeight) - 1, 1);
                                            o.FramesCount = Math.Max(o.Sprite.FramesCount - 1, 1);
                                            o.Sprite.cellW = s.CellHeight;
                                            o.Sprite.cellH = s.CellWidth;

                                            o.Position = new Vector2(vec.X - s.CellWidth / 2f, vec.Y - s.CellHeight / 2f);
                                            o.Sprite.ImageRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, s.CellWidth, s.CellHeight);
                                            o.LayerName = selectedLayer.Name;
                                            o.Layer = (ObjectLayer)selectedLayer;

                                            Sgml.currentObject = o;
                                            o.EvtCreate();

                                           o.Layer.Objects.Add(o);
                                            SceneObjects.Add(o);
                                            sh.RegisterObject(o);
 
                                        }

                                        if (!ks.IsKeyDown(Keys.LeftShift))
                                        {
                                            clickedObject = o;
                                        }

                                        
                                    }

                                    
                                }
                            }

                            if (!Sgml.PlaceEmpty(vec) && !ks.IsKeyDown(Keys.LeftShift))
                            {
                                // there's something cool at the position already, time to grab it
                                GameObject collidingObject = Sgml.instance_place(vec);

                                if (collidingObject != null)
                                {
                                    clickedObject = collidingObject;
                                    helpVec = new Vector2(-MousePositionTranslated.X + collidingObject.Position.X, -MousePositionTranslated.Y + collidingObject.Position.Y);
                                    clickedVec = MousePositionTranslated;
                                }
                            }
                        }
                    }
                    else
                    {
                        Vector2 m = MousePositionTranslated;

                        Tile alreadyT = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == (int)m.X / 32 && x.PosY == (int)m.Y / 32);

                        if (alreadyT == null)
                        { 
                            Tile t = new Tile();
                            t.Bits = 16;
                            t.DrawRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 32);
                            t.SourceTexture = currentAutotile.Texture;
                            t.PosX = (int) m.X / 32;
                            t.PosY = (int) m.Y / 32;
                            t.TileLayer = currentTileLayer;
                            t.TileLayerName = t.TileLayer.Name;

                            currentTileLayer.Tiles.Add(t);

                            t = Autotile.UpdateTile(t, currentTileLayer);

                            // basic 4
                            Tile t1 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX && x.PosY == t.PosY - 1); // N // 2
                            Tile t2 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX && x.PosY == t.PosY + 1); // S // 64
                            Tile t3 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX + 1 && x.PosY == t.PosY); // W // 16
                            Tile t4 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX - 1 && x.PosY == t.PosY); // S // 8

                            // extended 4
                            Tile t5 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX - 1 && x.PosY == t.PosY - 1); // EN // 1
                            Tile t6 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX + 1 && x.PosY == t.PosY - 1); // WN // 4
                            Tile t7 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX - 1 && x.PosY == t.PosY + 1); // ES // 32
                            Tile t8 = currentTileLayer.Tiles.FirstOrDefault(x => x.PosX == t.PosX + 1 && x.PosY == t.PosY + 1); // WS // 128

                            if (t1 != null) { Autotile.UpdateTile(t1, currentTileLayer); }
                            if (t2 != null) { Autotile.UpdateTile(t2, currentTileLayer); }
                            if (t3 != null) { Autotile.UpdateTile(t3, currentTileLayer); }
                            if (t4 != null) { Autotile.UpdateTile(t4, currentTileLayer); }

                            if (t5 != null) { Autotile.UpdateTile(t5, currentTileLayer); }
                            if (t6 != null) { Autotile.UpdateTile(t6, currentTileLayer); }
                            if (t7 != null) { Autotile.UpdateTile(t7, currentTileLayer); }
                            if (t8 != null) { Autotile.UpdateTile(t8, currentTileLayer); }
                        }
                    }
                }
                else
                {
                    vec = MousePositionTranslated;
                    vec = new Vector2(vec.X + helpVec.X, vec.Y + helpVec.Y);

                    if (DrawGrid)
                    {
                        vec = MousePositionTranslated;
                        vec.X -= (int)(clickedObject.Sprite.ImageRectangle.Width - 32) / 2;//16;
                        vec.Y -= (int)(clickedObject.Sprite.ImageRectangle.Height - 32) / 2;

                        vec = new Vector2((int)vec.X / 32 * 32, (int)vec.Y / 32 * 32);
                    }

                    if (!cmsOpen)
                    {
                        clickedObject.Position = vec;
                    }
                }
            }
            else if (mb == MouseButtons.Right)
            {
              //  Debug.WriteLine("----");
                Vector2 vec = MousePositionTranslated;
                if (DrawGrid)
                {
                    vec = new Vector2((int)vec.X / 32 * 32, (int)vec.Y / 32 * 32);
                }

                foreach (RoomLayer rl in roomLayers)
                {
                    if (rl.Visible)
                    {
                        if (rl is ObjectLayer)
                        {
                            ObjectLayer ol = (ObjectLayer) rl;
                            for (var i = ol.Objects.Count - 1; i >= 0; i--)
                            {

                                Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle((int)ol.Objects[i].Position.X, (int)ol.Objects[i].Position.Y, ol.Objects[i].Sprite.ImageRectangle.Width, ol.Objects[i].Sprite.ImageRectangle.Height);

                                if (ks.IsKeyDown(Keys.LeftShift) && r.Contains(vec))
                                {
                                    SceneObjects.Remove(ol.Objects[i]);
                                    ol.Objects[i].EvtDelete();
                                    ol.Objects.Remove(ol.Objects[i]);
                                }
                            }
                        }
                    }
                }
            }

            MousePrevious = MousePositionTranslated;
        }

        public void RightClickMenuSelected(ToolStripItemClickedEventArgs e)
        {
            goodBoy = true;
            if (clickedObject != null)
            {
                clickedObject.EvtContextMenuSelected(e.ClickedItem);
                clickedObject = null;
            }
        }

        public void MoveView()
        {
            if (panView)
            {
                cam.TargetPosition = new Vector2(cam.Position.X + helpVec.X - MousePositionTranslated.X, cam.Position.Y + helpVec.Y - MousePositionTranslated.Y);
            }
        }

        public void ClearAll()
        {
            SceneObjects.Clear();
            roomLayers.Clear();

        }

        public void ClearNodes()
        {
            if (lt.dtv.Nodes.Count > 0)
            {
                lt.dtv.Nodes[0].Nodes.Clear();
            }
        }

        public void PropagateNodes()
        {
            GameRoom gr = (GameRoom)Activator.CreateInstance(currentRoom.GetType());
            selectedLayer = gr.Layers[0];
            int currentDepth = 0;
            foreach (RoomLayer rl in gr.Layers)
            {
                DarkTreeNode dtn = new DarkTreeNode(rl.Name);
                dtn.Tag = dtn;
                dtn.Tag = "";

                if (rl.LayerType == RoomLayer.LayerTypes.typeObject)
                {
                    dtn.Icon = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("WorldLocal_16x");
                }
                else if (rl.LayerType == RoomLayer.LayerTypes.typeAsset)
                {
                    dtn.Icon = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("Image_16x");
                }
                else
                {
                    dtn.Icon = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("MapLineLayer_16x");
                }

                rl.Depth = currentDepth;
                currentDepth += 100;
                roomLayers.Add(rl);
                lt?.dtv.Nodes[0].Nodes.Add(dtn);
            }
        }

        public void LoadGame(string path)
        {
            bool flag = false;

            // First we fuck current scene
            ClearAll();


            // Load layers
            if (lt.dtv.Nodes.Count > 0)
            {
                lt.dtv.Nodes[0].Nodes.Clear();
            }

            // Prepare XML serializer
            // Then we load raw data
            Root root = new Root();
            XmlSerializer ser = new XmlSerializer(typeof(Root), Form1.reflectedTypes.ToArray());
            StreamReader w = new StreamReader(path);
            Root rawData = (Root)ser.Deserialize(w);

            // First load back room itself
            Form1.width = (int)rawData.Room.Size.X;
            Form1.height = (int)rawData.Room.Size.Y;
            Form1.ActiveForm.Text = "Simplex RPG Engine / " + rawData.Room.Name;

            currentRoom = rawData.Room;

            // if (currentRoom != null)
            // {
            GameRoom gr = (GameRoom)Activator.CreateInstance(currentRoom.GetType());
            selectedLayer = gr.Layers[0];
            int currentDepth = 0;
            foreach (RoomLayer rl in gr.Layers)
            {
                DarkTreeNode dtn = new DarkTreeNode(rl.Name);
                dtn.Tag = dtn;
                dtn.Tag = "";

                if (rl.LayerType == RoomLayer.LayerTypes.typeObject)
                {
                    dtn.Icon = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("WorldLocal_16x");
                }
                else if (rl.LayerType == RoomLayer.LayerTypes.typeAsset)
                {
                    dtn.Icon = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("Image_16x");
                }
                else
                {
                    dtn.Icon = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("MapLineLayer_16x");
                }

                rl.Depth = currentDepth;
                currentDepth += 100;
                roomLayers.Add(rl);
                lt?.dtv.Nodes[0].Nodes.Add(dtn);
            }

            // we need to initialize layers by type
            foreach (RoomLayer rl in roomLayers)
            {
                if (rl.LayerType == RoomLayer.LayerTypes.typeTile)
                {
                    // Start with empty cell data and load stuff later on
                    ((TileLayer)rl).Data = new int[(int)currentRoom.Size.X / 32, (int)currentRoom.Size.Y / 32];

                    // Now select correct tileset and assign it to this.. well tileset
                    Tileset tl = tilesets.FirstOrDefault(x => x.Name == ((TileLayer)rl).TilelistName);

                    // this can fail so check for that
                    if (tl != null)
                    {
                        // also we need to load textures for the tileset
                       // tl.AutotileLib = 

                        // all good
                        ((TileLayer)rl).Tileset = tl;
                    }
                }
            }


                // Time to load babies
                foreach (GameObject g in rawData.Objects)
                {
                    Spritesheet s = Sprites.FirstOrDefault(x => x.Name == g.Sprite.TextureSource);
              
                    g.Sprite.Texture = s.Texture;
                    g.Sprite.ImageRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, s.CellWidth, s.CellHeight);
                    g.Sprite.TextureRows = s.Rows;
                    g.Sprite.TextureCellsPerRow = s.Texture.Width / s.CellWidth;
                    g.Sprite.ImageSize = new Vector2(s.CellWidth, s.CellHeight);
                    g.Sprite.FramesCount = (s.Texture.Width / s.CellWidth) * (s.Texture.Height / s.CellHeight) - 1;
                    g.FramesCount = g.Sprite.FramesCount - 1;
                    g.Sprite.cellW = s.CellHeight;
                    g.Sprite.cellH = s.CellWidth;

                    RoomLayer rt = roomLayers.FirstOrDefault(x => x.Name == g.LayerName);
                    g.Layer = (ObjectLayer) rt;

                    g.Sprite.UpdateImageRectangle();
                    g.UpdateState();               
                    g.UpdateColliders();

                    // Need to give object OriginalType !!!
                    g.OriginalType = Type.GetType(g.TypeString);

                    g.EvtCreate();
                    g.EvtLoad();

                    g.Layer.Objects.Add(g);
                    sh.RegisterObject(g);
                    
                }

                // Load tiles
                foreach (Tile t in rawData.Tiles)
                {
                    RoomLayer correctLayer = roomLayers.FirstOrDefault(x => x.Name == t.TileLayerName);

                    if (correctLayer != null)
                    {
                        TileLayer crt = (TileLayer) correctLayer;
                        t.TileLayer = crt;
                        crt.Tiles.Add(t);
                    }
                    else
                    {
                        Debug.WriteLine("Tile could not be loaded - TileLayerName is wrong");
                    }
                }

                // Now update all tiles
                Texture2D ttt = Editor.Content.Load<Texture2D>(Path.GetFullPath("../../../SimplexRpgEngine3/Content/bin/Windows/Sprites/Tilesets/" + "tileset0"));

                foreach (RoomLayer rl in roomLayers)
                {
                    if (rl is TileLayer)
                    {
                        TileLayer crt = (TileLayer)rl;

                        foreach (Tile t in crt.Tiles)
                        {
                          t.SourceTexture = ttt;
                          Autotile.UpdateTile(t, crt);
                        }
                    }
                }

           w.Close();
        }

        public void cmsClosed()
        {
            cmsOpen = false;
            clickedObject = null;
        }

        public void cmsOpened()
        {
            cmsOpen = true;
        }
    }
}

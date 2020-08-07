using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using StillDesign.PhysX.MathPrimitives;

namespace StillDesign.PhysX.Samples
{
    public partial class TearingCloth : Sample
    {
        private Cloth _clothL;
        private Cloth _clothR;

        private DateTime _keyboardDelay;

        private List<Actor> AnchorActorsL = new List<Actor>();
        private List<Actor> AnchorActorsR = new List<Actor>();
        private float windX = 0, windY = 0, windZ = 0;

        private SlimDX.Direct3D10.Font font;
        private SlimDX.Direct3D10.Sprite sprite;
        //SlimDX.Direct3D10.Mesh mesh;

        string MessageText = "H:Help Esc:Exit J:日本語表示";

        bool IsJapanese = false;

        public TearingCloth()
        {
            _keyboardDelay = DateTime.MinValue;

            Run();
        }

        protected override void LoadContent()
        {
            font = new SlimDX.Direct3D10.Font(Engine.GraphicsDevice, 12, "ＭＳ ゴシック");
            sprite = new SlimDX.Direct3D10.Sprite(Engine.GraphicsDevice, 100);
            Engine.Camera.View = SlimDX.Matrix.LookAtLH(new SlimDX.Vector3(0, 20, -40), new SlimDX.Vector3(0, 20, 0), new SlimDX.Vector3(0, 1, 0));
        }

        protected override void Update(TimeSpan elapsed)
        {
            ProcessKeyboard();
        }

        protected override void Draw()
        {
            string outmessage = "";
            outmessage += "Wind (x,y,z)=( " + windX + " , " + windY + " , " + windZ + " )\n" + MessageText;

            sprite.Begin(SlimDX.Direct3D10.SpriteFlags.SaveState);
            font.Draw(sprite, outmessage, new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(1000, 200)), SlimDX.Direct3D10.FontDrawFlags.Left, new SlimDX.Color4(255, 255, 255));
            sprite.End();
            Engine.GraphicsDevice.ClearState();

            //mesh.SetIndexData(new SlimDX.DataStream(inds, false, false), inds.Count());
            //mesh.SetPointRepresentationData(new SlimDX.DataStream(poss,false,false));
        }

        protected override void LoadPhysics(Scene scene)
        {
            int w = 26;
            int h = 26;

            float hw = w / 2.0f;
            float hh = h / 2.0f;

            Vector3 p = new Vector3(0, h + 1, 0);

            // Create a Grid of Points
            int vertices, indices;

            ClothMesh clothMesh;
            {
                var grid = VertexGrid.CreateGrid(w, h);

                vertices = grid.Points.Length;
                indices = grid.Indices.Length;

                ClothMeshDescription clothMeshDesc = new ClothMeshDescription();
                clothMeshDesc.AllocateVertices<Vector3>(vertices);
                clothMeshDesc.AllocateTriangles<int>(indices / 3);

                clothMeshDesc.VertexCount = vertices;
                clothMeshDesc.TriangleCount = indices / 3;

                clothMeshDesc.VerticesStream.SetData(grid.Points);
                clothMeshDesc.TriangleStream.SetData(grid.Indices);

                // We are using 32 bit integers for our indices, so make sure the 16 bit flag is removed.
                // 32 bits are the default, so this isn't technically needed, but it's good to show in a sample
                clothMeshDesc.Flags &= ~MeshFlag.Indices16Bit;
                clothMeshDesc.Flags |= (MeshFlag)((int)clothMeshDesc.Flags | (int)ClothMeshFlag.Tearable);

                //var elements = new[]
                //{
                //new SlimDX.Direct3D10.InputElement("Position", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 0, 0),
                //new SlimDX.Direct3D10.InputElement("Color", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 16, 0)
                //};
                //mesh = new SlimDX.Direct3D10.Mesh(Engine.GraphicsDevice, elements, "Position", vertices, indices / 3, SlimDX.Direct3D10.MeshFlags.Has32BitIndices);

                // Write the cooked data to memory
                using (var memoryStream = new MemoryStream())
                {
                    Cooking.InitializeCooking();
                    Cooking.CookClothMesh(clothMeshDesc, memoryStream);
                    Cooking.CloseCooking();

                    // Need to reset the position of the stream to the beginning
                    memoryStream.Position = 0;

                    clothMesh = Engine.Core.CreateClothMesh(memoryStream);
                }
            }

            //

            int j = vertices * 2;
            int k = indices * 3;

            var clothDesc = new ClothDescription()
            {
                ClothMesh = clothMesh,
                GlobalPose =
                    Matrix.RotationX((float)Math.PI / 2.0F) *
                    Matrix.Translation(-w - 1, 0, 0) *
                    Matrix.Translation(p),
                Flags = ClothFlag.Gravity | ClothFlag.Bending | ClothFlag.CollisionTwoway | ClothFlag.Visualization,
                BendingStiffness = 0.1f,
                TearFactor = 1.5f,
                WindAcceleration = new Vector3(windX, windY, windZ)
            };
            clothDesc.MeshData.AllocatePositions<Vector3>(j);
            clothDesc.MeshData.AllocateIndices<int>(k);
            clothDesc.MeshData.AllocateNormals<Vector3>(j);

            clothDesc.MeshData.MaximumVertices = j;
            clothDesc.MeshData.MaximumIndices = k;

            clothDesc.MeshData.NumberOfVertices = vertices;
            clothDesc.MeshData.NumberOfIndices = indices;

            _clothL = scene.CreateCloth(clothDesc);

            var clothDesc2 = new ClothDescription()
            {
                ClothMesh = clothMesh,
                GlobalPose =
                    Matrix.RotationX((float)Math.PI / 2.0F) *
                    Matrix.Translation(1, 0, 0) *
                    Matrix.Translation(p),
                Flags = ClothFlag.Gravity | ClothFlag.Bending | ClothFlag.CollisionTwoway | ClothFlag.Visualization,
                BendingStiffness = 0.1f,
                TearFactor = 1.5f,
                WindAcceleration = new Vector3(windX, windY, windZ)
            };
            clothDesc2.MeshData.AllocatePositions<Vector3>(j);
            clothDesc2.MeshData.AllocateIndices<int>(k);
            clothDesc2.MeshData.AllocateNormals<Vector3>(j);

            clothDesc2.MeshData.MaximumVertices = j;
            clothDesc2.MeshData.MaximumIndices = k;

            clothDesc2.MeshData.NumberOfVertices = vertices;
            clothDesc2.MeshData.NumberOfIndices = indices;

            _clothR = scene.CreateCloth(clothDesc2);
            //


            for (int i = 0; i <= w; i += 2)
            {
                var actorDesc = new ActorDescription()
                {
                    GlobalPose = Matrix.Translation(new Vector3(i - w - 1, 0, 0) + p),
                    Shapes = { new SphereShapeDescription(0.3F) },
                    BodyDescription = new BodyDescription(3)
                };
                var actor = scene.CreateActor(actorDesc);
                AnchorActorsL.Add(actor);
                _clothL.AttachToShape(actor.Shapes.First(), (ClothAttachmentFlag)0);
            }
            for (int i = 0; i <= w; i += 2)
            {
                var actorDesc = new ActorDescription()
                {
                    GlobalPose = Matrix.Translation(new Vector3(-i + w + 1, 0, 0) + p),
                    Shapes = { new SphereShapeDescription(0.3F) },
                    BodyDescription = new BodyDescription(3)
                };
                var actor = scene.CreateActor(actorDesc);
                AnchorActorsR.Add(actor);
                _clothR.AttachToShape(actor.Shapes.First(), (ClothAttachmentFlag)0);
            }
            for (int i = 0; i <= 1; i++)
            {
                var actorDesc = new ActorDescription()
                {
                    GlobalPose = Matrix.Translation(new Vector3(0, -1, 0.2F * (float)Math.Pow(-1, i)) + p),
                    Shapes = { new BoxShapeDescription(new Vector3(2 * w + 4, 0.001F, 0.1F)) }
                };
                scene.CreateActor(actorDesc);
            }
            for (int i = 0; i <= 1; i++)
            {
                var actorDesc = new ActorDescription()
                {
                    GlobalPose = Matrix.Translation(new Vector3(0, -1, 0.4F * (float)Math.Pow(-1, i)) + p),
                    Shapes = { new BoxShapeDescription(new Vector3(2 * w + 4, 1, 0.1F)) }
                };
                scene.CreateActor(actorDesc);
            }
            for (int i = 0; i <= 1; i++)
            {
                var actorDesc = new ActorDescription()
                {
                    GlobalPose = Matrix.Translation(new Vector3((w + 2.1F) * (float)Math.Pow(-1, i), -1, 0) + p),
                    Shapes = { new BoxShapeDescription(new Vector3(0.1F, 1, 1)) }
                };
                scene.CreateActor(actorDesc);
            }
        }

        private void CreateBox(bool pm)
        {
            var random = new Random();

            int w = random.Next(1, 8);
            int h = random.Next(1, 8);
            int d = random.Next(1, 8);

            int x = random.Next(-26, 26);
            int y = random.Next(0, 26);

            int vx = random.Next(0, 10);
            int vy = random.Next(0, 5);
            int vz = random.Next(5, 25);

            var desc = new ActorDescription(new BoxShapeDescription(w, h, d))
            {
                BodyDescription = new BodyDescription(100),
                GlobalPose =
                    Matrix.RotationX((float)(random.NextDouble() * 2 * Math.PI - Math.PI / 2.0F * (pm ? 1 : -1))) *
                    Matrix.Translation(x, y, (pm ? 10 : -10)),
            };

            var actor = Engine.Scene.CreateActor(desc);
            actor.LinearVelocity = new Vector3(vx, vy, -vz * (pm ? 1 : -1));
        }

        private void ProcessKeyboard()
        {
            if (DateTime.Now - _keyboardDelay > TimeSpan.FromMilliseconds(100))
            {
                if (Engine.Keyboard.IsKeyDown(Keys.B))
                {
                    CreateBox(true);

                    MessageText = IsJapanese ? "手前へ箱を投げます。" : "Throwing the box towards you.";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.N))
                {
                    CreateBox(false);

                    MessageText = IsJapanese ? "奥へ箱を投げます。" : "Throwing the box to the back.";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.V))
                {
                    //Open Curtain
                    for (int i = 0; i < AnchorActorsL.Count(); i++)
                    {
                        AnchorActorsL[i].LinearVelocity = new Vector3(-3F * i / AnchorActorsL.Count(), 0, 0);
                        AnchorActorsR[i].LinearVelocity = new Vector3(+3F * i / AnchorActorsR.Count(), 0, 0);
                    }
                    MessageText = IsJapanese ? "カーテンを開けます(全ボール)。" : "Opening the curtain (using all balls).";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.C))
                {
                    //Close Curtain
                    for (int i = 0; i < AnchorActorsL.Count(); i++)
                    {
                        AnchorActorsL[i].LinearVelocity = new Vector3(+3F * i / AnchorActorsL.Count(), 0, 0);
                        AnchorActorsR[i].LinearVelocity = new Vector3(-3F * i / AnchorActorsR.Count(), 0, 0);
                    }
                    MessageText = IsJapanese ? "カーテンを閉じます(全ボール)。" : "Closing the curtain (using all balls).";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.G))
                {
                    //Open Curtain with 2 ball
                    AnchorActorsL[AnchorActorsL.Count() - 1].LinearVelocity = new Vector3(-3, 0, 0);
                    AnchorActorsR[AnchorActorsR.Count() - 1].LinearVelocity = new Vector3(3, 0, 0);
                    MessageText = IsJapanese ? "カーテンを開けます(端のボール)。" : "Opening the curtain (using two balls).";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.F))
                {
                    //Close Curtain with 2 ball
                    AnchorActorsL[AnchorActorsL.Count() - 1].LinearVelocity = new Vector3(3, 0, 0);
                    AnchorActorsR[AnchorActorsR.Count() - 1].LinearVelocity = new Vector3(-3, 0, 0);
                    MessageText = IsJapanese ? "カーテンを閉じます(端のボール)。" : "Closing the curtain (using two balls).";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.H))
                {
                    _keyboardDelay = DateTime.Now;
                    MessageText = IsJapanese ?
                        "[ヘルプ] ASDW:移動 3:風停止 E:風ランダム 4R5T6Y:風をX(4R)Y(5T)Z(6Y)方向に増(456)減(RTY)\n         VCGF:カーテンを開閉(VC:全てのボールで,GF:端のボールで) KL:ボール停止(K:全て,L:端のみ)\n         BN:箱を投げる(B:手前へ,N:奥へ) Z:保存 H:ヘルプ  Esc:終了" :
                        "[Help] ASDW:Move the camera 3:Stop the wind E:Set wind random\n       4R5T6Y:Add(456)/Subtract(RTY) X(4R)Y(5T)Z(6Y) value from wind.\n       VCGF:Open/Close the curtain(VC:using all balls,GF:using two balls) KL:Stop balls(K:All,L:Two)\n       BN:Throw box(B:towards you,N:to the back) Z:Save H:Help  Esc:Exit";
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.K))
                {
                    //Stop all ball
                    for (int i = 0; i < AnchorActorsL.Count(); i++)
                    {
                        AnchorActorsL[i].LinearVelocity = new Vector3(0, 0, 0);
                        AnchorActorsR[i].LinearVelocity = new Vector3(0, 0, 0);
                    }
                    MessageText = IsJapanese ? "ボール停止(全て)。" : "Stoping all balls.";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.L))
                {
                    //Stop 2 ball
                    AnchorActorsL[AnchorActorsL.Count() - 1].LinearVelocity = new Vector3(0, 0, 0);
                    AnchorActorsR[AnchorActorsR.Count() - 1].LinearVelocity = new Vector3(0, 0, 0);
                    MessageText = IsJapanese ? "ボール停止(端)。" : "Stoping two balls.";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.D4))
                {
                    windX += 1.0F;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese? "風 - X方向 - 増。":"Wind - X - Increase";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.R))
                {
                    windX -= 1.0F;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - X方向 - 減。" : "Wind - X - Decrease";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.D5))
                {
                    windY += 1.0F;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - Y方向 - 増。" : "Wind - Y - Increase";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.T))
                {
                    windY -= 1.0F;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - Y方向 - 減。" : "Wind - Y - Decrease";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.D6))
                {
                    windZ += 1.0F;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - Z方向 - 増。" : "Wind - Z - Increase";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.Y))
                {
                    windZ -= 1.0F;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - Z方向 - 減。" : "Wind - Z - Decrease";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.D3))
                {
                    windX = 0;
                    windY = 0;
                    windZ = 0;
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - 停止。" : "Wind - Stop";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.E))
                {
                    var random = new Random();
                    windX = random.Next(-15, 15);
                    windY = random.Next(-15, 15);
                    windZ = random.Next(-15, 15);
                    _clothL.WindAcceleration = new Vector3(windX, windY, windZ);
                    _clothR.WindAcceleration = new Vector3(windX, windY, windZ);
                    MessageText = IsJapanese ? "風 - ランダム。" : "Wind - Random";
                    _keyboardDelay = DateTime.Now;
                }
                else if (Engine.Keyboard.IsKeyDown(Keys.Z))
                {
                    int[] indsl = _clothL.GetMeshData().IndicesStream.GetData<int>();
                    Vector3[] possl = _clothL.GetMeshData().PositionsStream.GetData<Vector3>();
                    int[] indsr = _clothR.GetMeshData().IndicesStream.GetData<int>();
                    Vector3[] possr = _clothR.GetMeshData().PositionsStream.GetData<Vector3>();

                    int num = 0;
                    string filename;

                    do { filename = "cloth" + num + ".x"; num++; } while (System.IO.File.Exists(filename));

                    StreamReader sr = new StreamReader("xhead.txt", System.Text.Encoding.GetEncoding("Shift_JIS"));
                    string xhead = sr.ReadToEnd();
                    sr.Close();
                    using (System.IO.StreamWriter sw = new StreamWriter(filename, false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                    {
                        sw.Write(xhead);

                        sw.WriteLine("Mesh mesh0 {");
                        sw.WriteLine("\t" + possl.Count() + ";");
                        int i;
                        for (i = 0; i < possl.Count() - 1; i++)
                        {
                            sw.WriteLine("\t"
                                + possl[i].X.ToString() + ";"
                                + possl[i].Y.ToString() + ";"
                                + possl[i].Z.ToString() + ";,");
                        }
                        sw.WriteLine("\t"
                            + possl[i].X.ToString() + ";"
                            + possl[i].Y.ToString() + ";"
                            + possl[i].Z.ToString() + ";;");

                        sw.WriteLine("");
                        sw.WriteLine("\t" + (indsl.Count() / 3) + ";");
                        for (i = 0; i < indsl.Count() - 3; i += 3)
                        {
                            sw.WriteLine("\t3;" + indsl[i] + "," + indsl[i + 1] + "," + indsl[i + 2] + ";,");
                        }
                        sw.WriteLine("\t3;" + indsl[i] + "," + indsl[i + 1] + "," + indsl[i + 2] + ";;");

                        sw.WriteLine("}");
                        sw.WriteLine("");

                        sw.WriteLine("Mesh mesh1 {");
                        sw.WriteLine("\t" + possr.Count() + ";");
                        for (i = 0; i < possr.Count() - 1; i++)
                        {
                            sw.WriteLine("\t"
                                + possr[i].X.ToString() + ";"
                                + possr[i].Y.ToString() + ";"
                                + possr[i].Z.ToString() + ";,");
                        }
                        sw.WriteLine("\t"
                            + possr[i].X.ToString() + ";"
                            + possr[i].Y.ToString() + ";"
                            + possr[i].Z.ToString() + ";;");

                        sw.WriteLine("");
                        sw.WriteLine("\t" + (indsr.Count() / 3) + ";");
                        for (i = 0; i < indsr.Count() - 3; i += 3)
                        {
                            sw.WriteLine("\t3;" + indsr[i] + "," + indsr[i + 1] + "," + indsr[i + 2] + ";,");
                        }
                        sw.WriteLine("\t3;" + indsr[i] + "," + indsr[i + 1] + "," + indsr[i + 2] + ";;");

                        sw.WriteLine("}");
                    }



                    MessageText = IsJapanese ? "カーテンの状態をファイル:" + (filename) + "に保存しました。" : "Saved the curtain :" + (filename);
                    _keyboardDelay = DateTime.Now;

                }
                else if (Engine.Keyboard.IsKeyDown(Keys.J))
                {
                    _keyboardDelay = DateTime.Now;
                    IsJapanese = !IsJapanese;
                    MessageText = IsJapanese ? "日本語モード" : "English mode";
                }

            }
        }
    }
}
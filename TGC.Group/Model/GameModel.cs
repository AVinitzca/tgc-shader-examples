using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using TGC.Core;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using TGC.Core.Shaders;
using System.Collections.Generic;
using System;
using TGC.Core.Text;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    /// 
    public class GameModel : TgcExample
    {
        private TgcMesh sphere;
        private float rotation = 0;

        private TGCVector3 effectVector;

        private float time = 0;

        private List<string> techniques = new List<string>
        {
            "RenderMesh",
            "Expansion",
            "Extrude",
            "IdentityPlaneExtrude",
            "PlanarExtrude",
            "TextureCycling",
            "ColorCycling",
            "InnerLight",
            "ExtrudeCombined",
        };
        
        float sumValue = 0;
        float sumFactor = 1;

        private Microsoft.DirectX.Direct3D.Effect effect;

        /*
         * 
         * Esto no sirve a fines de la clase, solo para su funcionamiento
         * 
         */

        private TgcText2D currentTechnique;
        
        int technique = 0;

        private TgcArrow arrow;

        private TGCVector3 scale;

        private bool wireframe = false;

        float vectorRot = 0;

        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }


        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;

            this.initVector();

            TGCVector3 center = new TGCVector3(0, 0, 0);

            this.setEffects(center);
            
            var lava = TgcTexture.createTexture(d3dDevice, MediaDir + "lava.jpg");
            this.createText(d3dDevice.Viewport.Width, d3dDevice.Viewport.Height);
            this.createSphereMesh(lava, center);

            this.scale = new TGCVector3(20, 20, 20);

            var cameraPosition = new TGCVector3(0, 0, 125);
            //Quiero que la camara mire hacia el origen (0,0,0).
            var lookAt = TGCVector3.Empty;
            //Configuro donde esta la posicion de la camara y hacia donde mira.
            Camara.SetCamera(cameraPosition, lookAt);
            //Internamente el framework construye la matriz de view con estos dos vectores.
            //Luego en nuestro juego tendremos que crear una cámara que cambie la matriz de view con variables como movimientos o animaciones de escenas.
        }

        private void createText(int width, int height)
        {
            this.currentTechnique = new TgcText2D();
            this.currentTechnique.Position = new Point(width - 200, height - 100);
            this.currentTechnique.Size = new Size(100, 1000);
            this.currentTechnique.Color = Color.DarkRed;
        }

        private void createSphereMesh(TgcTexture texture, TGCVector3 center)
        {
            var rawSphere = new TGCSphere(1, texture, center);
            rawSphere.LevelOfDetail = 4;
            rawSphere.updateValues();
            this.sphere = rawSphere.toMesh("Mesh");

            this.sphere.Effect = this.effect;
            this.sphere.Technique = this.techniques[this.technique];
        }

        private void setEffects(TGCVector3 center)
        {
            this.effect = TgcShaders.loadEffect(Game.Default.ShadersDirectory + "BasicShader.fx");
            this.effect.SetValue("center", new[] { center.X, center.Y, center.Z, 1f });
        }

        public override void Update()
        {
            PreUpdate();

            this.handleInput();

            this.time += ElapsedTime;

            this.sphere.Transform = TGCMatrix.Scaling(this.scale) * TGCMatrix.RotationYawPitchRoll(this.rotation, 0, 0);;

            this.updateEffectVector();

            this.effect.SetValue("time", this.time);

            this.effect.SetValue("effectVector", new[] { this.effectVector.X, this.effectVector.Y, this.effectVector.Z});

            this.effect.SetValue("factor", (float)Math.Round(sumValue, 2));

            PostUpdate();
        }

        public override void Render()
        {
            PreRender();

            if (this.wireframe)
            {
                D3DDevice.Instance.Device.RenderState.FillMode = FillMode.WireFrame;
                D3DDevice.Instance.Device.RenderState.Lighting = false;
            }
            this.sphere.Render();            
            if( this.wireframe)
            {
                D3DDevice.Instance.Device.RenderState.FillMode = FillMode.Solid;
                D3DDevice.Instance.Device.RenderState.Lighting = true;
            }

            this.arrow.Render();
            this.currentTechnique.render();

            PostRender();
        }

        private void handleInput()
        {
            //Capturar Input Mouse
            if (Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                //Como ejemplo podemos hacer un movimiento simple de la cámara.
                //En este caso le sumamos un valor en Y
                Camara.SetCamera(Camara.Position + new TGCVector3(0, 10f, 0), Camara.LookAt);
                //Ver ejemplos de cámara para otras operaciones posibles.

                //Si superamos cierto Y volvemos a la posición original.
                if (Camara.Position.Y > 300f)
                {
                    Camara.SetCamera(new TGCVector3(Camara.Position.X, 0f, Camara.Position.Z), Camara.LookAt);
                }
            }

            if(Input.keyPressed(Key.Q))
            {
                this.technique++;
                if(this.technique > this.techniques.Count - 1)
                {
                    this.technique = 0;
                }
                this.sphere.Technique = this.techniques[this.technique];
                this.currentTechnique.Text = this.sphere.Technique;
            }


            if (Input.keyPressed(Key.U))
            {
                this.toggleWireframe();
            }

            if (Input.keyPressed(Key.I))
            {
                this.toggleArrow();
            }

            if (Input.keyDown(Key.O))
            {
                this.rotation += FastMath.QUARTER_PI / 2 * this.ElapsedTime;
            }

            if(Input.keyDown(Key.UpArrow) && (this.sumValue + this.sumFactor * ElapsedTime) < 1)
            {
                this.sumValue += this.sumFactor * ElapsedTime;
            }
            else if(Input.keyDown(Key.DownArrow) && this.sumValue - this.sumFactor * ElapsedTime > 0f)
            {
                this.sumValue -= this.sumFactor * ElapsedTime;
            }

            if(Input.keyDown(Key.Z))
            {
                this.effectVector.Y += 0.01f;
            }

            if (Input.keyDown(Key.X))
            {
                this.effectVector.Y -= 0.01f;
            }

            if (Input.keyDown(Key.C))
            {
                this.vectorRot += FastMath.QUARTER_PI * ElapsedTime * 7;
                this.effectVector.X = (float)Math.Round(FastMath.Cos(this.vectorRot) * 20, 4);
                this.effectVector.Z = (float)Math.Round(FastMath.Sin(this.vectorRot) * 20, 4);
            }

            if (Input.keyDown(Key.V))
            {
                this.vectorRot -= FastMath.QUARTER_PI * ElapsedTime * 7;
                this.effectVector.X = (float)Math.Round(FastMath.Cos(this.vectorRot) * 20, 4);
                this.effectVector.Z = (float)Math.Round(FastMath.Sin(this.vectorRot) * 20, 4);
            }

        }



        private void initVector()
        {
            this.arrow = new TgcArrow();
            arrow.HeadColor = Color.Blue;
            arrow.BodyColor = Color.Red;
            arrow.Thickness = 1;
            arrow.Enabled = false;
            arrow.HeadSize = new TGCVector2(1, 1);
            arrow.updateValues();
            this.effectVector = new TGCVector3(1, 1, 1);
        }

        private void updateEffectVector()
        {
            this.arrow.PStart = TGCVector3.Empty;
            this.arrow.PEnd = this.effectVector;
            this.arrow.updateValues();
        }

        private void toggleWireframe()
        {
            this.wireframe = !this.wireframe;
            if(!this.wireframe)
            {
                D3DDevice.Instance.Device.RenderState.FillMode = FillMode.Solid;
                D3DDevice.Instance.Device.RenderState.Lighting = true;
            }
            else if (this.wireframe)
            {
                
                D3DDevice.Instance.Device.RenderState.FillMode = FillMode.WireFrame;
                D3DDevice.Instance.Device.RenderState.Lighting = false;
            }
        }

        private void toggleArrow()
        {
            this.arrow.Enabled = !this.arrow.Enabled;
        }


        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            this.sphere.Dispose();
            this.arrow.Dispose();
            this.currentTechnique.Dispose();
            this.effect.Dispose();
        }
    }
}
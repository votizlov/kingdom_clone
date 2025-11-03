
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FluidSim2DProject
{

    public class FluidSim : MonoBehaviour
    {

        public Color m_fluidColor = Color.red;

        public Color m_obstacleColor = Color.white;
      
        public Material m_guiMat, m_advectMat, m_buoyancyMat, m_divergenceMat, m_jacobiMat, m_impluseMat, impulseFromTexMat, m_gradientMat, m_obstaclesMat,obstaclesFromTexMat, visualizationMat;

        RenderTexture m_guiTex, m_divergenceTex, m_obstaclesTex;
        RenderTexture[] m_velocityTex, m_densityTex, m_pressureTex, m_temperatureTex;

        [SerializeField]float m_impulseTemperature = 10.0f;
        [SerializeField]float m_impulseDensity = 1.0f;
        [SerializeField]float m_temperatureDissipation = 0.99f;
        [SerializeField]float m_velocityDissipation = 0.99f;
        [SerializeField]float m_densityDissipation = 0.9999f;
        [SerializeField]float m_ambientTemperature = 0.0f;
        [SerializeField]float m_smokeBuoyancy = 1.0f;
        [SerializeField]float m_smokeWeight = 0.05f;

        [SerializeField]float m_cellSize = 1.0f;
        [SerializeField]float m_gradientScale = 1.0f;

        Vector2 m_inverseSize;
        [SerializeField]int m_numJacobiIterations = 50;

        [SerializeField]Vector2 m_implusePos = new Vector2(0.5f, 0.0f);
        [SerializeField]float m_impluseRadius = 0.1f;
        [SerializeField]float m_mouseImpluseRadius = 0.05f;

        [SerializeField]Vector2 m_obstaclePos = new Vector2(0.5f, 0.5f);
        [SerializeField]float m_obstacleRadius = 0.1f;

        [SerializeField]Rect m_rect;
        [SerializeField]int m_width, m_height;
        [SerializeField] private RenderTexture fluidCameraOutput;
        [SerializeField] private bool isImpulseFromTex = false;
        [SerializeField] private bool isDrawDebugGUI = false;
        public Texture customImpulseTex;

        public void DebugToggleImpulseFromTex(InputAction.CallbackContext context)
        {
            isImpulseFromTex = !isImpulseFromTex;
        }
 
        void Start()
        {
            m_width = Mathf.Max(1, Screen.width / 2);
            m_height = Mathf.Max(1, Screen.height / 2);

            Vector2 size = new Vector2(m_width, m_height);
            Vector2 pos = new Vector2(Screen.width / 2, Screen.height / 2) - size * 0.5f;
            m_rect = new Rect(pos, size);

            m_inverseSize = new Vector2(1.0f / m_width, 1.0f / m_height);

            m_velocityTex = new RenderTexture[2];
            m_densityTex = new RenderTexture[2];
            m_temperatureTex = new RenderTexture[2];
            m_pressureTex = new RenderTexture[2];

            CreateSurface(m_velocityTex, RenderTextureFormat.RGFloat, FilterMode.Bilinear);
            CreateSurface(m_densityTex, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateSurface(m_temperatureTex, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateSurface(m_pressureTex, RenderTextureFormat.RFloat, FilterMode.Point);

            m_guiTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.ARGB32);
            m_guiTex.filterMode = FilterMode.Bilinear;
            m_guiTex.wrapMode = TextureWrapMode.Clamp;
            m_guiTex.Create();
            visualizationMat.SetTexture("_ProjectionTexture", m_guiTex);

            m_divergenceTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_divergenceTex.filterMode = FilterMode.Point;
            m_divergenceTex.wrapMode = TextureWrapMode.Clamp;
            m_divergenceTex.Create();

            m_obstaclesTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_obstaclesTex.filterMode = FilterMode.Point;
            m_obstaclesTex.wrapMode = TextureWrapMode.Clamp;
            m_obstaclesTex.Create();
        }

        void OnGUI()
        {
            if(isDrawDebugGUI)
                GUI.DrawTexture(m_rect, m_guiTex);
        }

        void CreateSurface(RenderTexture[] surface, RenderTextureFormat format, FilterMode filter)
        {
            surface[0] = new RenderTexture(m_width, m_height, 0, format, RenderTextureReadWrite.Linear);
            surface[0].filterMode = filter;
            surface[0].wrapMode = TextureWrapMode.Clamp;
            surface[0].Create();

            surface[1] = new RenderTexture(m_width, m_height, 0, format, RenderTextureReadWrite.Linear);
            surface[1].filterMode = filter;
            surface[1].wrapMode = TextureWrapMode.Clamp;
            surface[1].Create();
        }

        void Advect(RenderTexture velocity, RenderTexture source, RenderTexture dest, float dissipation, float timeStep)
        {
            m_advectMat.SetVector("_InverseSize", m_inverseSize);
            m_advectMat.SetFloat("_TimeStep", timeStep);
            m_advectMat.SetFloat("_Dissipation", dissipation);
            m_advectMat.SetTexture("_Velocity", velocity);
            m_advectMat.SetTexture("_Source", source);
            m_advectMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_advectMat);
        }

        void ApplyBuoyancy(RenderTexture velocity, RenderTexture temperature, RenderTexture density, RenderTexture dest, float timeStep)
        {
            m_buoyancyMat.SetTexture("_Velocity", velocity);
            m_buoyancyMat.SetTexture("_Temperature", temperature);
            m_buoyancyMat.SetTexture("_Density", density);
            m_buoyancyMat.SetFloat("_AmbientTemperature", m_ambientTemperature);
            m_buoyancyMat.SetFloat("_TimeStep", timeStep);
            m_buoyancyMat.SetFloat("_Sigma", m_smokeBuoyancy);
            m_buoyancyMat.SetFloat("_Kappa", m_smokeWeight);

            Graphics.Blit(null, dest, m_buoyancyMat);
        }

        void ApplyImpulse(RenderTexture source, RenderTexture dest, Vector2 pos, float radius, float val)
        {
            m_impluseMat.SetVector("_Point", pos);
            m_impluseMat.SetFloat("_Radius", radius);
            m_impluseMat.SetFloat("_Fill", val);
            m_impluseMat.SetTexture("_Source", source);

            Graphics.Blit(null, dest, m_impluseMat);
        }
        
        void ApplyImpulseFromTex(RenderTexture source, RenderTexture dest, Texture impulse,float val)
        {
            impulseFromTexMat.SetTexture("_Source", source);
            impulseFromTexMat.SetTexture("_MainTex", impulse);
            impulseFromTexMat.SetFloat("_Fill", val);

            Graphics.Blit(null, dest, impulseFromTexMat);
        }

        void ComputeDivergence(RenderTexture velocity, RenderTexture dest)
        {
            m_divergenceMat.SetFloat("_HalfInverseCellSize", 0.5f / m_cellSize);
            m_divergenceMat.SetTexture("_Velocity", velocity);
            m_divergenceMat.SetVector("_InverseSize", m_inverseSize);
            m_divergenceMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_divergenceMat);
        }

        void Jacobi(RenderTexture pressure, RenderTexture divergence, RenderTexture dest)
        {

            m_jacobiMat.SetTexture("_Pressure", pressure);
            m_jacobiMat.SetTexture("_Divergence", divergence);
            m_jacobiMat.SetVector("_InverseSize", m_inverseSize);
            m_jacobiMat.SetFloat("_Alpha", -m_cellSize * m_cellSize);
            m_jacobiMat.SetFloat("_InverseBeta", 0.25f);
            m_jacobiMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_jacobiMat);
        }

        void SubtractGradient(RenderTexture velocity, RenderTexture pressure, RenderTexture dest)
        {
            m_gradientMat.SetTexture("_Velocity", velocity);
            m_gradientMat.SetTexture("_Pressure", pressure);
            m_gradientMat.SetFloat("_GradientScale", m_gradientScale);
            m_gradientMat.SetVector("_InverseSize", m_inverseSize);
            m_gradientMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_gradientMat);
        }

        void AddObstacles()
        {
            m_obstaclesMat.SetVector("_InverseSize", m_inverseSize);
            m_obstaclesMat.SetVector("_Point", m_obstaclePos);
            m_obstaclesMat.SetFloat("_Radius", m_obstacleRadius);

            Graphics.Blit(null, m_obstaclesTex, m_obstaclesMat);
        }

        void AddObstaclesFromCameraRT()
        {
            obstaclesFromTexMat.SetTexture("_MainTex",fluidCameraOutput);

            Graphics.Blit(null, m_obstaclesTex, obstaclesFromTexMat);
        }

        void ClearSurface(RenderTexture surface)
        {
            Graphics.SetRenderTarget(surface);
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.SetRenderTarget(null);
        }

        void Swap(RenderTexture[] texs)
        {
            RenderTexture temp = texs[0];
            texs[0] = texs[1];
            texs[1] = temp;
        }

        Vector2 ConvertScreenToSimulation(Vector2 screenPos)
        {
            screenPos.y = Screen.height - screenPos.y;
            screenPos = Rect.PointToNormalized(m_rect, screenPos);
            screenPos.y = 1.0f - screenPos.y;

            screenPos.x = Mathf.Clamp01(screenPos.x);
            screenPos.y = Mathf.Clamp01(screenPos.y);

            return screenPos;
        }

        void FixedUpdate()
        {
            //Obstacles only need to be added once unless changed.
            AddObstaclesFromCameraRT();

            //Set the density field and obstacle color.
            m_guiMat.SetColor("_FluidColor", m_fluidColor);
            m_guiMat.SetColor("_ObstacleColor", m_obstacleColor);

            int READ = 0;
            int WRITE = 1;
            float dt = 0.125f;

            //Advect velocity against its self
            Advect(m_velocityTex[READ], m_velocityTex[READ], m_velocityTex[WRITE], m_velocityDissipation, dt);
            //Advect temperature against velocity
            Advect(m_velocityTex[READ], m_temperatureTex[READ], m_temperatureTex[WRITE], m_temperatureDissipation, dt);
            //Advect density against velocity
            Advect(m_velocityTex[READ], m_densityTex[READ], m_densityTex[WRITE], m_densityDissipation, dt);

            Swap(m_velocityTex);
            Swap(m_temperatureTex);
            Swap(m_densityTex);

            //Determine how the flow of the fluid changes the velocity
            ApplyBuoyancy(m_velocityTex[READ], m_temperatureTex[READ], m_densityTex[READ], m_velocityTex[WRITE], dt);

            Swap(m_velocityTex);

            //Refresh the impluse of density and temperature
            //ApplyImpulseFromTex(m_temperatureTex[READ], m_temperatureTex[WRITE],fluidCameraOutput);
            //ApplyImpulseFromTex(m_densityTex[READ], m_densityTex[WRITE], fluidCameraOutput);

            if (isImpulseFromTex)
            {
                //ApplyImpulseFromTex(m_temperatureTex[READ], m_temperatureTex[WRITE],customImpulseTex);
                //ApplyImpulseFromTex(m_densityTex[READ], m_densityTex[WRITE], customImpulseTex);
                ApplyImpulseFromTex(m_temperatureTex[READ], m_temperatureTex[WRITE],fluidCameraOutput,m_impulseTemperature);
                ApplyImpulseFromTex(m_densityTex[READ], m_densityTex[WRITE], fluidCameraOutput,m_impulseDensity);
            }
            else
            {
                ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], m_implusePos, m_impluseRadius, m_impulseTemperature);
                ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], m_implusePos, m_impluseRadius, m_impulseDensity);
            }
            
            Swap(m_temperatureTex);
            Swap(m_densityTex);

            //If left click down add impluse, if right click down remove impulse from mouse pos.
            if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                Vector2 pos = ConvertScreenToSimulation(Input.mousePosition);

                float sign = (Input.GetMouseButton(0)) ? 1.0f : -1.0f;

                ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], pos, m_mouseImpluseRadius, m_impulseTemperature);
                ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], pos, m_mouseImpluseRadius, m_impulseDensity * sign);

                Swap(m_temperatureTex);
                Swap(m_densityTex);
            }

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed)
                    {
                        continue;
                    }

                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Canceled || phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    {
                        continue;
                    }

                    Vector2 pos = ConvertScreenToSimulation(touch.position.ReadValue());

                    ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], pos, m_mouseImpluseRadius, m_impulseTemperature);
                    ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], pos, m_mouseImpluseRadius, m_impulseDensity);

                    Swap(m_temperatureTex);
                    Swap(m_densityTex);
                }
            }

            //Calculates how divergent the velocity is
            ComputeDivergence(m_velocityTex[READ], m_divergenceTex);

            ClearSurface(m_pressureTex[READ]);

            int i = 0;
            for (i = 0; i < m_numJacobiIterations; ++i)
            {
                Jacobi(m_pressureTex[READ], m_divergenceTex, m_pressureTex[WRITE]);
                Swap(m_pressureTex);
            }

            //Use the pressure tex that was last rendered into. This computes divergence free velocity
            SubtractGradient(m_velocityTex[READ], m_pressureTex[READ], m_velocityTex[WRITE]);

            Swap(m_velocityTex);

            //Render the tex you want to see into gui tex. Will only use the red channel
            m_guiMat.SetTexture("_Obstacles", m_obstaclesTex);
            Graphics.Blit(m_densityTex[READ], m_guiTex, m_guiMat);
        }
    }
    

}

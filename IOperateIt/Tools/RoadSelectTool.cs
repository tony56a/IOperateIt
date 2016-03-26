using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IOperateIt.Manager;
using IOperateIt.Utils;
using UnityEngine;

namespace IOperateIt.Tools
{
    class RoadSelectTool: DefaultTool
    {

        Texture2D texture;
        protected override void Awake()
        {
            // Create a new 2x2 texture ARGB32 (32 bit with alpha) and no mipmaps
            texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            // set the pixel values
            texture.SetPixel(0, 0, new Color(0f, 1.0f, 0f, 0.15f));
            texture.SetPixel(1, 0, new Color(0f, 1.0f, 0f, 0.15f));
            texture.SetPixel(0, 1, new Color(0f, 1.0f, 0f, 0.15f));
            texture.SetPixel(1, 1, new Color(0f, 1.0f, 0f, 0.15f));

            // Apply all SetPixel calls
            texture.Apply();

            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

        }

        protected override void OnToolUpdate()
        {
            if (m_toolController != null && !m_toolController.IsInsideUI && Cursor.visible)
            {
                RaycastOutput raycastOutput;

                if (RaycastRoad(out raycastOutput))
                {
                    ushort netSegmentId = raycastOutput.m_netSegment;

                    if (netSegmentId != 0)
                    {           

                        NetManager netManager = Singleton<NetManager>.instance;
                        NetSegment netSegment = netManager.m_segments.m_buffer[(int)netSegmentId];
                       
                        if (netSegment.m_flags.IsFlagSet(NetSegment.Flags.Created))
                        {
                            if (Event.current.type == EventType.MouseDown /*&& Event.current.button == (int)UIMouseButton.Left*/)
                            {
                                ShowToolInfo(false, null, new Vector3());
                                /*GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                                cylinder.GetComponent<Collider>().enabled = false;
                                cylinder.transform.position = netSegment.m_middlePosition;
                                cylinder.transform.localScale = new Vector3(50, 200, 50);
                                Material material = new Material(Shader.Find("Unlit/Transparent"));
                                material.SetInt("_Mode", 3);
                                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                material.SetInt("_ZWrite", 0);
                                material.DisableKeyword("_ALPHATEST_ON");
                                material.DisableKeyword("_ALPHABLEND_ON");
                                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                                material.renderQueue = 3000;
                                material.mainTexture =texture;
                                cylinder.GetComponent<Renderer>().sharedMaterial = material;*/

                                VehicleInfo info = VehicleHolder.getInstance().getVehicleInfo();
                                VehicleHolder.getInstance().setActive(netSegment.m_middlePosition,Vector3.zero);

                                //unset self as tool
                                ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<DefaultTool>();
                                ToolsModifierControl.SetTool<DefaultTool>();
                                UnityEngine.Object.Destroy(this);
                            }
                            else
                            {
                                ShowToolInfo(true, "Spawn Vehicle", netSegment.m_bounds.center);
                            }
                        }
                    }
                }
            }
            else
            {
                ShowToolInfo(false, null, new Vector3());
            }
        }

        bool RaycastRoad(out RaycastOutput raycastOutput)
        {
            RaycastInput raycastInput = new RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
            raycastInput.m_netService.m_service = ItemClass.Service.Road;
            raycastInput.m_netService.m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels;
            raycastInput.m_ignoreSegmentFlags = NetSegment.Flags.None;
            raycastInput.m_ignoreNodeFlags = NetNode.Flags.None;
            raycastInput.m_ignoreTerrain = true;

            return RayCast(raycastInput, out raycastOutput);
        }
    }
}

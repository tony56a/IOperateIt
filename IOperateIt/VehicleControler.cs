using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using IOperateIt.Manager;
using IOperateIt.Utils;
using UnityEngine;
using IOperateIt.UIUtils;

namespace IOperateIt
{
    class ColliderContainer
    {
        public GameObject colliderOwner;
        public MeshCollider meshCollider;
        public BoxCollider boxCollider;
    }

    enum CameraType
    {
        normal,
        closeUp,
        fps,
    }

    public class VehicleControler : MonoBehaviour
    {
        private readonly int NUM_BUILDING_COLLIDERS = 360 / 10;
        private readonly int NUM_VEHICLE_COLLIDERS = 160;
        private readonly int NUM_PARKED_VEHICLE_COLLIDERS = 80;

        private readonly float SCAN_DISTANCE = 500f;

        private bool active;

        BuildingManager buildingManager;
        NetManager netManager;
        TerrainManager terrainManager;
        OptionsManager optionsManager;
        VehicleManager vehicleManager;
        AudioManager audioManager;
        AudioManager.ListenerInfo listenerInfo;

        private Rigidbody vehicleRigidBody;
        private Mesh vehicleMesh;
        private BoxCollider vehicleCollider;
        private List<Light> mLights;
        private List<SoundEffect> audioEffects = new List<SoundEffect>();
        private List<SoundEffect> togglableAudioEffects = new List<SoundEffect>();

        private float terrainHeight;
        private Vector3 prevPosition;

        private CameraController cameraController;
        private Camera camera;
        private CameraType cameraType = CameraType.normal;
        private float fpsCameraOffsetXAxis = 2.75f;
        private float fpsCameraOffsetYAxis = 1.5f;

        private ColliderContainer[] mBuildingColliders;
        private ColliderContainer[] mVehicleColliders;
        private ColliderContainer[] mParkedVehicleColliders;


        InstanceID id = new InstanceID();
        void Awake()
        {
            // Get manager instances
            netManager = Singleton<NetManager>.instance;
            terrainManager = Singleton<TerrainManager>.instance;
            buildingManager = Singleton<BuildingManager>.instance;
            optionsManager = OptionsManager.Instance();
            vehicleManager = Singleton<VehicleManager>.instance;
            audioManager = Singleton<AudioManager>.instance;
            listenerInfo = ReflectionUtils.ReadPrivate<AudioManager, AudioManager.ListenerInfo>(audioManager, "m_listenerInfo");

            // Get controller for camera
            cameraController = GameObject.FindObjectOfType<CameraController>();
            camera = cameraController.GetComponent<Camera>();

            // Initialize the lights for the vehicle
            mLights = new List<Light>(2);

            // Set up colliders for nearby buildings, vehicles,
            mBuildingColliders = new ColliderContainer[NUM_BUILDING_COLLIDERS];
            mVehicleColliders = new ColliderContainer[NUM_VEHICLE_COLLIDERS];
            mParkedVehicleColliders = new ColliderContainer[NUM_PARKED_VEHICLE_COLLIDERS];

            for (int i =0; i<NUM_BUILDING_COLLIDERS; i++)
            {
                //Initialize the building collider
                ColliderContainer buildingCollider = new ColliderContainer();
                buildingCollider.colliderOwner = new GameObject("buildingCollider"+i);
                buildingCollider.meshCollider = buildingCollider.colliderOwner.AddComponent<MeshCollider>();
                buildingCollider.meshCollider.convex = true;
                buildingCollider.meshCollider.enabled = true;
                mBuildingColliders[i] = buildingCollider;
            }

            for( int i =0; i<NUM_VEHICLE_COLLIDERS; i++)
            {
                ColliderContainer vehicleCollider = new ColliderContainer();
                vehicleCollider.colliderOwner = new GameObject("vehicleCollider" + i);
                vehicleCollider.boxCollider = vehicleCollider.colliderOwner.AddComponent<BoxCollider>();
                vehicleCollider.boxCollider.enabled = true;
                mVehicleColliders[i] = vehicleCollider;


            }

            for( int i = 0; i<NUM_PARKED_VEHICLE_COLLIDERS; i++)
            {
                ColliderContainer parkedVehicleCollider = new ColliderContainer();
                parkedVehicleCollider.colliderOwner = new GameObject("parkedVehicleCollider" + i);
                parkedVehicleCollider.boxCollider = parkedVehicleCollider.colliderOwner.AddComponent<BoxCollider>();
                parkedVehicleCollider.boxCollider.enabled = true;
                mParkedVehicleColliders[i] = parkedVehicleCollider;
            }
            id.RawData = 123456789;

        }

        void Update()
        {
            if (this.active && Input.GetKey(KeyCode.F2))
            {
                setInactive();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                cameraType = (CameraType)(((int)cameraType + 1) % Enum.GetNames(typeof(CameraType)).Length);
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                foreach (Light light in mLights)
                {
                    light.enabled = !light.enabled;
                }
            }
        }

        public void setActive(Vector3 position, VehicleInfo vehicleInfo, Vector3 rotation)
        {
            if (!this.active)
            {
                this.active = true;
            }
            spawnVehicle(position, rotation, vehicleInfo);
            cameraController.enabled = false;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.75f;

        }

        private void spawnVehicle(Vector3 position,Vector3 rotation, VehicleInfo vehicleInfo)
        {
            NetManager manager = Singleton<NetManager>.instance;
            TerrainManager terrainManager = Singleton<TerrainManager>.instance;
            Vector3[] hitPos = new Vector3[2];
            ushort nodeIndex;
            ushort segmentIndex;
            bool rayCastSuccess;

            gameObject.transform.parent = (Transform)null;
            gameObject.transform.position = position;
            gameObject.transform.eulerAngles = new Vector3(rotation.x, rotation.y);
            vehicleMesh = vehicleInfo.m_mesh;
            gameObject.AddComponent<MeshFilter>().mesh = vehicleMesh;
            gameObject.AddComponent<MeshRenderer>().material = vehicleInfo.m_material;
            gameObject.SetActive(true);

            int numLights = vehicleInfo.m_lightPositions.Length > 1 ? 2 : vehicleInfo.m_lightPositions.Length;
            for( int i = 0; i< numLights; i++ )
            {
                Vector3 lightPosition = vehicleInfo.m_lightPositions[i];
                GameObject lightObj = new GameObject();
                Light light = lightObj.AddComponent<Light>();
                lightObj.transform.parent = gameObject.transform;
                lightObj.transform.localPosition = lightPosition;
                light.type = LightType.Spot;
                light.enabled = false;
                light.spotAngle = 20f;
                light.range = 50f;
                light.intensity = 5f;
                light.color = Color.white;
                mLights.Add(light);
            }

            this.vehicleRigidBody = gameObject.AddComponent<Rigidbody>();
            this.vehicleRigidBody.isKinematic = false;
            this.vehicleRigidBody.useGravity = false;
            this.vehicleRigidBody.drag = 2f;
            this.vehicleRigidBody.angularDrag = 2.5f;
            this.vehicleRigidBody.freezeRotation = true;
            this.vehicleCollider = gameObject.AddComponent<BoxCollider>();
            this.vehicleCollider.size = this.vehicleMesh.bounds.size + new Vector3(0,1.3f,0);

            Segment3 ray = new Segment3(position + new Vector3(0f, 1.5f, 0f), position + new Vector3(0f, -100f, 0f));
            rayCastSuccess = manager.RayCast(null, ray, 0f, ItemClass.Service.Road, ItemClass.Service.PublicTransport, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos[0], out nodeIndex, out segmentIndex);
            rayCastSuccess = rayCastSuccess || manager.RayCast(null, ray, 0f, ItemClass.Service.Beautification, ItemClass.Service.Water, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos[1], out nodeIndex, out segmentIndex);
            terrainHeight = terrainManager.SampleDetailHeight(transform.position);
            terrainHeight = Mathf.Max(terrainHeight, Mathf.Max(hitPos[0].y, hitPos[1].y));

            if (position.y < terrainHeight + 1)
            {
                transform.position = new Vector3(position.x, terrainHeight, position.z);
            }
            prevPosition = transform.position;

            audioEffects.Clear();
            togglableAudioEffects.Clear();
            foreach (VehicleInfo.Effect effect in vehicleInfo.m_effects)
            {
                SoundEffect soundEffect = effect.m_effect as SoundEffect;

                if( soundEffect != null)
                {
                    if ((effect.m_vehicleFlagsRequired & (Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2)) != Vehicle.Flags.None)
                    {
                        togglableAudioEffects.Add(soundEffect);
                    }
                    else
                    {
                        audioEffects.Add(soundEffect);
                    }
                }
            
            }
        }

        void OnCollisionEnter(Collision col)
        {
        }

        public void setInactive()
        {
            if (this.active)
            {
                this.active = false;
                cameraController.enabled = true;
                unSpawnVehicle();
            }
        }

        private void unSpawnVehicle()
        {
            for(int i = 0; i < NUM_BUILDING_COLLIDERS; i++)
            {
                UnityEngine.Object.Destroy(mBuildingColliders[i].colliderOwner);
            }

            for( int i =0; i<NUM_VEHICLE_COLLIDERS; i++)
            {
                UnityEngine.Object.Destroy(mVehicleColliders[i].colliderOwner);
            }
            for(int i = 0; i < NUM_PARKED_VEHICLE_COLLIDERS; i++)
            {
                UnityEngine.Object.Destroy(mParkedVehicleColliders[i].colliderOwner);
            }
            UnityEngine.Object.Destroy(gameObject);
        }

        private void LateUpdate()
        {
        }

        private void updateBuildingColliders()
        {
            Vector3 segmentVector;
            Segment3 circularScanSegment;
            ushort buildingIndex;
            Vector3 hitPos = Vector3.zero;
            HashSet<ushort> hitBuildings = new HashSet<ushort>();
            Building building;
            Vector3 buildingPosition;
            Quaternion buildingRotation;

            for (int i = 0; i < NUM_BUILDING_COLLIDERS; i++)
            {
                float x  = (float)Math.Cos(Mathf.Deg2Rad * (10f*i ));
                float z = (float)Math.Sin(Mathf.Deg2Rad * (10f * i));
                segmentVector = new Vector3(x, 1f, z) * SCAN_DISTANCE;
                circularScanSegment = new Segment3(transform.position,
                                                   transform.position + segmentVector);

                buildingManager.RayCast(circularScanSegment, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default, Building.Flags.None, out hitPos, out buildingIndex);
                if (hitPos != Vector3.zero)
                {
                    building = buildingManager.m_buildings.m_buffer[buildingIndex];
                    if (!hitBuildings.Contains(buildingIndex) && building.Info.m_class.m_service != ItemClass.Service.Road &&
                        building.Info.name != "478820060.CableStay32m_Data" && building.Info.name != "BridgePillar.CableStay32m_Data")
                    {
                        building.CalculateMeshPosition(out buildingPosition, out buildingRotation);
                        mBuildingColliders[i].meshCollider.sharedMesh = building.Info.m_mesh;
                        mBuildingColliders[i].colliderOwner.transform.position = buildingPosition;
                        mBuildingColliders[i].colliderOwner.transform.rotation = buildingRotation;
                        hitBuildings.Add(buildingIndex);
                    }
                }
            }
        }

        private void setCollider(ushort vehicleId, int colliderIndex, bool isParked = false)
        {
            Vector3 vehiclePosition;
            Quaternion vehicleRotation;
            if (isParked)
            {
                VehicleParked parkedVehicle = vehicleManager.m_parkedVehicles.m_buffer[vehicleId];
                vehicleRotation = parkedVehicle.m_rotation;
                vehiclePosition = parkedVehicle.m_position;
                mParkedVehicleColliders[colliderIndex].boxCollider.size = parkedVehicle.Info.m_mesh.bounds.size + new Vector3(0, 2.6f, 0);
                mParkedVehicleColliders[colliderIndex].colliderOwner.transform.position = vehiclePosition;
                mParkedVehicleColliders[colliderIndex].colliderOwner.transform.rotation = vehicleRotation;
            }
            else
            {
                Vehicle vehicle = vehicleManager.m_vehicles.m_buffer[vehicleId];
                vehicle.GetSmoothPosition(vehicleId, out vehiclePosition, out vehicleRotation);
                // add an arbitary height to make collider work better
                mVehicleColliders[colliderIndex].boxCollider.size = vehicle.Info.m_lodMesh.bounds.size + new Vector3( 0, 1.3f, 0 );
                mVehicleColliders[colliderIndex].colliderOwner.transform.position = vehiclePosition;
                mVehicleColliders[colliderIndex].colliderOwner.transform.rotation = vehicleRotation;
            }
            
        }

        private void updateVehicleColliders()
        {
            int gridX = Mathf.Clamp((int)((transform.position.x / 32.0) + 270.0), 0, 539);
            int gridZ = Mathf.Clamp((int)((transform.position.z / 32.0) + 270.0), 0, 539);
            int colliderCounter = 0;

            Vehicle vehicle;

            int index = gridZ * 540 + gridX;
            ushort vehicleId = vehicleManager.m_vehicleGrid[index];
            if( vehicleId != 0)
            {
                setCollider(vehicleId, colliderCounter);
                colliderCounter++;
                vehicle = vehicleManager.m_vehicles.m_buffer[vehicleId];

                while (vehicle.m_nextGridVehicle != 0 && colliderCounter < NUM_VEHICLE_COLLIDERS)
                {
                    setCollider(vehicle.m_nextGridVehicle, colliderCounter);
                    vehicle = vehicleManager.m_vehicles.m_buffer[vehicle.m_nextGridVehicle];
                    colliderCounter++;
                }
            }
            for( int i = colliderCounter; i< NUM_VEHICLE_COLLIDERS; i++)
            {
                mVehicleColliders[colliderCounter].boxCollider.transform.position = new Vector3(float.PositiveInfinity, float.PositiveInfinity);
            }

        }

        private void updateParkedVehicleColliders()
        {
            int gridX = Mathf.Clamp((int)((transform.position.x / 32.0) + 270.0), 0, 539);
            int gridZ = Mathf.Clamp((int)((transform.position.z / 32.0) + 270.0), 0, 539);
            int colliderCounter = 0;

            VehicleParked parkedVehicle;

            int index = gridZ * 540 + gridX;
            ushort vehicleId = vehicleManager.m_parkedGrid[index];
            if (vehicleId != 0)
            {
                setCollider(vehicleId, colliderCounter, isParked:true);
                colliderCounter++;
                parkedVehicle = vehicleManager.m_parkedVehicles.m_buffer[vehicleId];

                while (parkedVehicle.m_nextGridParked != 0 && colliderCounter < NUM_PARKED_VEHICLE_COLLIDERS)
                {
                    setCollider(parkedVehicle.m_nextGridParked, colliderCounter,isParked:true);
                    parkedVehicle = vehicleManager.m_parkedVehicles.m_buffer[parkedVehicle.m_nextGridParked];
                    colliderCounter++;
                }
            }
            for (int i = colliderCounter; i < NUM_PARKED_VEHICLE_COLLIDERS; i++)
            {
                mVehicleColliders[colliderCounter].boxCollider.transform.position = new Vector3(float.PositiveInfinity, float.PositiveInfinity);
            }
        }

        private void calculateSlope()
        {
            Vector3 oldEuler = transform.rotation.eulerAngles;
            Vector3 diffVector = (Vector3)(transform.position-prevPosition);
            Quaternion newRotation;
            if (diffVector.sqrMagnitude > 0.05f)
            {
                newRotation = Quaternion.LookRotation(diffVector);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(Math.Abs(newRotation.eulerAngles.x), oldEuler.y, 0), Time.deltaTime * 6.0f);
            }
            prevPosition = transform.position;
        }

        private void FixedUpdate()
        {

            if (this.vehicleRigidBody != null)
            {
               
                Vector3[] hitPos = new Vector3[4];
                ushort nodeIndex;
                ushort segmentIndex;
                bool rayCastSuccess;

                updateBuildingColliders();
                updateVehicleColliders();
                updateParkedVehicleColliders();

                Segment3 ray = new Segment3(transform.position + new Vector3(0f, 1.5f, 0f), transform.position + new Vector3(0f, -100f, 0f));

                rayCastSuccess = netManager.RayCast(null,ray, 0f, ItemClass.Service.Road, ItemClass.Service.PublicTransport, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos[0], out nodeIndex, out segmentIndex);
                rayCastSuccess = rayCastSuccess || netManager.RayCast(null,ray, 0f, ItemClass.Service.Beautification, ItemClass.Service.Water, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos[1], out nodeIndex, out segmentIndex);
                terrainHeight = terrainManager.SampleDetailHeight(transform.position);
                terrainHeight = Mathf.Max(terrainHeight, Mathf.Max(hitPos[0].y, hitPos[1].y));

                if (transform.position.y < terrainHeight + 1)
                {
                    this.vehicleRigidBody.velocity = new Vector3(this.vehicleRigidBody.velocity.x, 0, this.vehicleRigidBody.velocity.z);
                    transform.position = new Vector3(transform.position.x, terrainHeight, transform.position.z);
                }
                else
                {
                    this.vehicleRigidBody.AddRelativeForce(Physics.gravity*6f);
                }

                if (Input.GetKey(optionsManager.forwardKey))
                {
                    this.vehicleRigidBody.AddRelativeForce(Vector3.forward * optionsManager.mAccelerationForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

                }
                else if (Input.GetKey(optionsManager.backKey))
                {
                    this.vehicleRigidBody.AddRelativeForce(Vector3.back * optionsManager.mAccelerationForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                }
                if (Input.GetKey(optionsManager.leftKey) && this.vehicleRigidBody.velocity.sqrMagnitude > 0.05f)
                {
                    Vector3 normalisedVelocity = this.vehicleRigidBody.velocity.normalized;
                    Vector3 brakeVelocity = normalisedVelocity * optionsManager.mBreakingForce*0.58f;
                    this.vehicleRigidBody.AddForce(-brakeVelocity);

                    this.vehicleRigidBody.AddRelativeTorque(Vector3.down * 5f * Time.fixedDeltaTime, ForceMode.VelocityChange);
                }
                if (Input.GetKey(optionsManager.rightKey) && this.vehicleRigidBody.velocity.sqrMagnitude > 0.05f)
                {
                    Vector3 normalisedVelocity = this.vehicleRigidBody.velocity.normalized;
                    Vector3 brakeVelocity = normalisedVelocity * optionsManager.mBreakingForce * 0.58f; 
                    this.vehicleRigidBody.AddForce(-brakeVelocity);

                    this.vehicleRigidBody.AddRelativeTorque(Vector3.up * 5f * Time.fixedDeltaTime, ForceMode.VelocityChange);
                }


                if (this.vehicleRigidBody.velocity.sqrMagnitude > optionsManager.mMaxVelocitySquared)
                {
                    Vector3 normalisedVelocity = this.vehicleRigidBody.velocity.normalized;
                    Vector3 brakeVelocity = normalisedVelocity * optionsManager.mAccelerationForce;  
                    this.vehicleRigidBody.AddForce(-brakeVelocity);
                }

                calculateSlope();

                Vector3 forward;
                Vector3 up;
                Vector3 pos;
                Vector3 lookAt;
                Quaternion currentOrientation;
                switch (cameraType)
                {
                    case CameraType.closeUp:
                        forward = transform.rotation * Vector3.forward;
                        up = transform.rotation * Vector3.up;

                        pos = transform.position + (transform.forward * (optionsManager.mcloseupXAxisOffset - vehicleMesh.bounds.size.x)) + (transform.up * (optionsManager.mcloseupYAxisOffset + vehicleMesh.bounds.size.y));
                        camera.transform.position = pos;
                        lookAt = pos + (transform.rotation * Vector3.forward) * 1.0f;

                        currentOrientation = camera.transform.rotation;
                        camera.transform.LookAt(lookAt, Vector3.up);
                        break;
                    case CameraType.fps:
                        forward = transform.rotation * Vector3.forward;
                        up = transform.rotation * Vector3.up;

                        pos = transform.position + (transform.forward * fpsCameraOffsetXAxis) + (transform.up * fpsCameraOffsetYAxis);
                        camera.transform.position = pos;
                        lookAt = pos + (transform.rotation * Vector3.forward) * 1.0f;

                        currentOrientation = camera.transform.rotation;
                        camera.transform.LookAt(lookAt, Vector3.up);
                        camera.transform.rotation = Quaternion.Slerp(currentOrientation, camera.transform.rotation,
                            Time.deltaTime * 3.0f); 
                        break;
                    case CameraType.normal:
                    default:
                        camera.transform.position = transform.position + new Vector3(optionsManager.mcameraXAxisOffset, optionsManager.mcameraYAxisOffset, 0);
                        camera.transform.LookAt(transform);
                        break;
                }
            }

            foreach ( SoundEffect effect in audioEffects)
            {
                effect.PlaySound(id, listenerInfo, vehicleManager.m_audioGroup, transform.position, vehicleRigidBody.velocity, 100, 100, 1);

            }
        }
    }
}

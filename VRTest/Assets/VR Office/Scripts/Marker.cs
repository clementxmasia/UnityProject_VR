using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;

namespace ChiliGames.VROffice
{
    //This class sends a Raycast from the marker and detect if it's hitting the whiteboard (tag: Finish)
    [RequireComponent(typeof(XRGrabbablePun))]
    public class Marker : MonoBehaviour
    {
        private Whiteboard whiteboard;
        [SerializeField] private Transform drawingPoint;
        [SerializeField] private Renderer markerTip;
        private RaycastHit touch;
        private bool touching;
        private float drawingDistance = 0.015f;
        private Quaternion lastAngle;
        private PhotonView pv;
        private XRGrabbablePun grabbable;
        [SerializeField] private int penSize = 6;
        [SerializeField] private Color color = Color.blue;
        private bool grabbed;

        private void Awake() {
            pv = GetComponent<PhotonView>();
            grabbable = GetComponent<XRGrabbablePun>();
        }

        private void Start()
        {
            //Subscribe to grabbed and dropped events
            grabbable.selectEntered.AddListener(MarkerGrabbed);
            grabbable.selectExited.AddListener(MargerDropped);

            var block = new MaterialPropertyBlock();

            // You can look up the property by ID instead of the string to be more efficient.
            block.SetColor("_BaseColor", color);

            // You can cache a reference to the renderer to avoid searching for it.
            markerTip.SetPropertyBlock(block);
        }

        private void MarkerGrabbed(SelectEnterEventArgs arg0) {
            grabbed = true;
        }

        private void MargerDropped(SelectExitEventArgs arg0) {
            grabbed = false;
        }

        void Update()
        {
            //if the marker is not in possesion of the user, or is not grabbed, we don't run update.
            if (!pv.IsMine) return;
            if (!grabbed) return;

            //Cast a raycast to detect whiteboard.
            if (Physics.Raycast(drawingPoint.position, drawingPoint.up, out touch, drawingDistance))
            {
                //The whiteboard has the tag "Finish".
                if (touch.collider.CompareTag("Finish"))
                {
                    if (!touching)
                    {
                        touching = true;
                        //store angle so while drawing, marker doesn't rotate
                        lastAngle = transform.rotation;
                        whiteboard = touch.collider.GetComponent<Whiteboard>();
                    }
                    if (whiteboard == null) return;
                    //Send the rpc with the coordinates, pen size and color of marker in RGB.
                    whiteboard.pv.RPC("DrawAtPosition", RpcTarget.AllBuffered, new float[] { touch.textureCoord.x, touch.textureCoord.y }, penSize, new float[]{color.r, color.g, color.b});
                }
            }
            else if (whiteboard != null)
            {
                touching = false;
                whiteboard.pv.RPC("ResetTouch", RpcTarget.AllBuffered);
                whiteboard = null;
            }
        }

        private void OnDestroy() {
            if(grabbable != null) {
                grabbable.selectEntered.RemoveListener(MarkerGrabbed);
                grabbable.selectExited.RemoveListener(MargerDropped);
            }
        }

        private void LateUpdate()
        {
            if (!pv.IsMine) return;

            //lock rotation of marker when touching whiteboard.
            if (touching)
            {
                transform.rotation = lastAngle;
            }
        }
    }
}
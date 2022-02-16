using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace ChiliGames.VROffice
{
    public class Whiteboard : MonoBehaviourPunCallbacks
    {
        private int textureSize = 2048;
        private Texture2D texture;
        private Color[] brush;
        private Color[] deleteColor;
        private new Renderer renderer;
        private bool touchingLastFrame;
        private float lastX, lastY;
        private bool everyOthrFrame;

        [HideInInspector] public PhotonView pv;

        void Start()
        {
            renderer = GetComponent<Renderer>();
            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 3;

            renderer.material.mainTexture = texture;

            Color fillColor = Color.white;
            var fillColorArray = texture.GetPixels();

            for (var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = fillColor;
            }

            texture.SetPixels(fillColorArray);

            texture.Apply();

            pv = GetComponent<PhotonView>();

            deleteColor = Enumerable.Repeat(Color.white, textureSize * textureSize).ToArray();
        }

        //RPC sent by the Marker class so every user gets the information to draw in whiteboard.
        [PunRPC]
        public void DrawAtPosition(float[] pos, int _pensize, float[] _color)
        {
            int penSize = _pensize;
            int penSizeHalf = _pensize / 2;

            brush = Enumerable.Repeat((new Color(_color[0], _color[1], _color[2])), penSize * penSize).ToArray();

            int x = (int)(pos[0] * textureSize - penSizeHalf);
            int y = (int)(pos[1] * textureSize - penSizeHalf);

            //If last frame was not touching a marker, we don't need to lerp from last pixel coordinate to new, so we set the last coordinates to the new.
            if (!touchingLastFrame)
            {
                lastX = (float)x;
                lastY = (float)y;
                touchingLastFrame = true;
            }

            if (touchingLastFrame)
            {
                texture.SetPixels(x, y, penSize, penSize, brush);

                //Lerp last pixel to new pixel, so we draw a continuous line.
                for (float t = 0.01f; t < 1.00f; t += 0.1f)
                {
                    int lerpX = (int)Mathf.Lerp(lastX, (float)x, t);
                    int lerpY = (int)Mathf.Lerp(lastY, (float)y, t);
                    texture.SetPixels(lerpX, lerpY, penSize, penSize, brush);
                }
                //We apply the texture every other frame, so we improve performance.
                if (!everyOthrFrame)
                {
                    everyOthrFrame = true;
                }
                else if (everyOthrFrame)
                {
                    texture.Apply();
                    everyOthrFrame = false;
                }
            }

            lastX = (float)x;
            lastY = (float)y;
        }

        //Reset the state of the whiteboard, so it doesn't interpolate/lerp last pixels drawn.
        [PunRPC]
        public void ResetTouch()
        {
            touchingLastFrame = false;
        }

        //To clear the whiteboard.
        public void ClearWhiteboard()
        {
            pv.RPC("RPC_ClearWhiteboard", RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void RPC_ClearWhiteboard()
        {
            texture.SetPixels(deleteColor);
            texture.Apply();
        }

        //This code below is for sending the whiteboard state to new players joining the room. This is causing lag so it is a WIP. You can still actiavate it but it will lag for 1 second the master client when somebody joins.
        /*
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            if (PhotonNetwork.IsMasterClient)
            {
                pv.RPC("RPC_SendWhiteboard", newPlayer, texture.EncodeToPNG());
            }
        }

        [PunRPC]
        private void RPC_SendWhiteboard(byte[] receivedByte)
        {
            Texture2D receivedTexture = new Texture2D(1, 1);
            receivedTexture.LoadImage(receivedByte);
            ApplyReceivedTexture(receivedTexture);
        }


        void ApplyReceivedTexture(Texture2D texture)
        {
            GetComponent<Renderer>().material.mainTexture = texture;
        }
        */
    }
}

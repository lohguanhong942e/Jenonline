using UnityEngine;

namespace Vesperhold
{
    // Keeps a painted backdrop filling the screen and drifts it slower than the camera.
    public class ParallaxBackdrop : MonoBehaviour
    {
        RoomData room; float width; float factor = 0.35f;
        public void Init(RoomData data, Texture2D tex)
        {
            room = data;
            float height = data.h + 2f;
            width = height * tex.width / tex.height;
            transform.position = new Vector3(data.w / 2f, -data.h / 2f, 5);
        }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null || room == null) return;
            float x = room.w / 2f + (cam.transform.position.x - room.w / 2f) * factor;
            // Stretch horizontally if the picture is narrower than the visible area.
            float need = cam.orthographicSize * 2f * cam.aspect + 4f;
            float scale = Mathf.Max(1f, need / width);
            transform.position = new Vector3(x, cam.transform.position.y * 0.9f - room.h * 0.05f, 5);
            transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}

using UnityEngine;

namespace LongLiveKhioyen
{
    public class BillboardSprite : MonoBehaviour
    {
        private Transform mainCam;

        void Start()
        {
            if (Camera.main != null) mainCam = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (mainCam == null) return;

            transform.rotation = mainCam.rotation;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LongLiveKhioyen
{
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;

        void Start()
        {
            _mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (_mainCamera != null)
            {
             transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward, 
                 _mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}

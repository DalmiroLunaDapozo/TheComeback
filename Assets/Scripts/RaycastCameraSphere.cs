using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class RaycastCameraSphere : MonoBehaviour
{
    [SerializeField] private GameObject _camera;
    [SerializeField] private GameObject target;
    [SerializeField] private LayerMask myLayerMask;

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        RaycastHit hit;

        if (Physics.Raycast(_camera.transform.position, (target.transform.position - _camera.transform.position).normalized, out hit, Mathf.Infinity, myLayerMask))
        {
            if (hit.collider.gameObject.tag == "sphereMask")
            {
                target.transform.DOScale(0, 4);
            }
            else
            {
                target.transform.DOScale(2, 1);
            }
        }
    }
}

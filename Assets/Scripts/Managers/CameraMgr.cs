using UnityEngine;
using Cinemachine;

public class CameraMgr : MonoSingleton<CameraMgr> {
    [SerializeField] Camera m_camera;
    [SerializeField] CinemachineVirtualCamera m_cinemachineCam;

    public void Follow( Transform target ) {
        m_cinemachineCam.Follow = target;
    }
}

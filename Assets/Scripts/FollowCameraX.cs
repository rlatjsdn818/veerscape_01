using UnityEngine;

/// <summary>
/// 이 스크립트가 붙은 오브젝트는 항상 메인 카메라의 X 위치를 따라갑니다.
/// (Y와 Z 위치는 원래 자신의 위치를 유지합니다)
/// 
/// [사용법]
/// 1. 인스펙터에 있는 바닥(Floor)과 천장(Ceiling) 오브젝트에 이 스크립트를 추가합니다.
/// 2. 끝! 이제 플레이어가 아무리 앞으로 전진해도 바닥과 천장이 카메라와 함께 이동합니다.
/// </summary>
public class FollowCameraX : MonoBehaviour
{
    private Camera cam;
    private float initialOffsetX;

    void Start()
    {
        cam = Camera.main;
        if (cam != null)
        {
            // 카메라와 현재 오브젝트 사이의 초기 거리 간격을 기억합니다.
            initialOffsetX = transform.position.x - cam.transform.position.x;
        }
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            // Y, Z는 그대로 두고 X만 카메라를 따라가도록 업데이트
            transform.position = new Vector3(
                cam.transform.position.x + initialOffsetX,
                transform.position.y,
                transform.position.z
            );
        }
    }
}

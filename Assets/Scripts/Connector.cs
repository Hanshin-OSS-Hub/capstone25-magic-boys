using Unity.VisualScripting;
using UnityEngine;

public class Connector : MonoBehaviour
{

    public Vector2 size = Vector2.one * 4f;
    public bool isConnected = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 halfsize = size * 0.5f;
        Vector3 offset = transform.position + transform.up * halfsize.y;
        Gizmos.DrawLine(offset, offset + transform.forward);

        //define top & side vectors
        Vector3 top = transform.up * size.y;
        Vector3 side = transform.right * halfsize.x;

        //define corner vectors
        Vector3 topRight = transform.position + top + side;
        Vector3 topLeft = transform.position + top - side;
        Vector3 bottomRight = transform.position + side;
        Vector3 bottomLeft = transform.position - side;
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        //draw diagnal lines
        Gizmos.color *= 0.7f;
        Gizmos.DrawLine(topRight, bottomLeft);
        Gizmos.DrawLine(topLeft, bottomRight);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

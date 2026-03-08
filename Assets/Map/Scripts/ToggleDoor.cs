using UnityEngine;

public class ToggleDoor : MonoBehaviour
{

    Animator myAinm;
    bool isInZone;

    void Start()
    {
        myAinm = GetComponent<Animator>();
    }

    void Update()
    {
     if(isInZone && Input.GetKeyDown(KeyCode.E))
        {
            bool isOpen = myAinm.GetBool("isOpen");
            myAinm.SetBool("isOpen", !isOpen);

        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            isInZone = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            isInZone = false;
        }
    }
}

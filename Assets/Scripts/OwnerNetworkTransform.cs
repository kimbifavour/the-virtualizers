using Unity.Netcode.Components;
using UnityEngine;

public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // false = owner-authoritative
    }

}
//public class NewMonoBehaviourScript : MonoBehaviour {
//    [DisallowMultipleComponent]

//}

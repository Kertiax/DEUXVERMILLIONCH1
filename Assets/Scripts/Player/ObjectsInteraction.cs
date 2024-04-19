using UnityEngine.InputSystem;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ObjectsInteraction : NetworkBehaviour
{
    private GameObject heldObject;
    private bool isHoldingObject = false;
    private GameObject head;
    private InputAction interact;
    private InputAction throwAction;
    private PlayerInputActions playerControls;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }
    private void Awake()
    {
        playerControls = new PlayerInputActions();
    }
    private void Start()
    {
        SetInteractInput();
        SetThrowInput();
    }

    private void SetInteractInput()
    {
        interact = playerControls.Player.Interact;
        interact.Enable();
        interact.performed += InteractFront;
    }

    private void SetThrowInput()
    {
        throwAction = playerControls.Player.Throw;
        throwAction.Enable();
        throwAction.performed += ThrowObject;
    }


    void GrabObject(GameObject obj)
    {
        PickupObjectServerRpc(obj.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ServerRpc]
    public void PickupObjectServerRpc(ulong objToPickupID)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup);
        if (objectToPickup == null || objectToPickup.transform.parent != null) return;

        if (objectToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            var pickUpObjectRigidbody = objectToPickup.GetComponent<Rigidbody>();
            pickUpObjectRigidbody.isKinematic = true;
            pickUpObjectRigidbody.interpolation = RigidbodyInterpolation.None;
            objectToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
            isHoldingObject = true;
            heldObject = objectToPickup.gameObject;
        }
    }

    [ServerRpc]
    public void DropObjectServerRpc()
    {
        if (heldObject != null)
        {
            heldObject.transform.parent = null;
            var pickedUpObjectRigidbody = heldObject.GetComponent<Rigidbody>();
            pickedUpObjectRigidbody.isKinematic = false;
            pickedUpObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            heldObject.GetComponent<NetworkTransform>().InLocalSpace = false;
            heldObject = null;
            isHoldingObject = false;
        }

    }

    void ReleaseObject()
    {
        if (heldObject != null)
        {
            heldObject.GetComponent<Rigidbody>().isKinematic = false;
            heldObject.transform.parent = null;
            isHoldingObject = false;
            heldObject = null;
            DropObjectServerRpc();
        }
    }

    void ThrowObject(InputAction.CallbackContext callbackContext)
    {
        if (heldObject != null)
        {
            Rigidbody rb = heldObject.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            heldObject.transform.parent = null;
            isHoldingObject = false;
            heldObject = null;
            rb.AddForce(head.transform.forward * 10f, ForceMode.Impulse);
            DropObjectServerRpc();
        }
    }

    public void Interact(InputAction.CallbackContext callbackContext)
    {
        if (!isHoldingObject)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 1f);
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("Grabbable"))
                {
                    GrabObject(collider.gameObject);
                    break;
                }
                else if (collider.CompareTag("Item"))
                {
                    InventoryManager.instance.AddItemToInventory(collider.GetComponent<Item>().inventoryItem);
                    RpcTest.instance.TestDespawnObjectRpc(collider.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }
        else
        {
            ReleaseObject();
        }
    }

    public float interactionRange = 5f;
    public LayerMask interactableLayer;

    public void InteractFront(InputAction.CallbackContext callbackContext)
    {
        if (!isHoldingObject)
        {
            RaycastHit hit;
            if (Physics.Raycast(GetComponent<PlayerMovement>().playerCamera.transform.position, GetComponent<PlayerMovement>().playerCamera.transform.forward, out hit, interactionRange, interactableLayer))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject.CompareTag("Grabbable"))
                {
                    GrabObject(hitObject);
                }
                else if (hitObject.CompareTag("Item"))
                {
                    InventoryManager.instance.AddItemToInventory(hitObject.GetComponent<Item>().inventoryItem);
                    RpcTest.instance.TestDespawnObjectRpc(hitObject.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }
        else
        {
            ReleaseObject();
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(GetComponent<PlayerMovement>().playerCamera.transform.position, GetComponent<PlayerMovement>().playerCamera.transform.forward * interactionRange);
    }
}

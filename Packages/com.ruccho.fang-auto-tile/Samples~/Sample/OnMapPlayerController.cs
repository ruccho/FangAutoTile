using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ruccho
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class OnMapPlayerController : MonoBehaviour
    {
        public float speedPerFrame;
        Vector2 currentPos;
        Vector2 targetPos;
        Vector2 velocity = Vector2.zero;
        Rigidbody2D rigidBody2DRef;

        // Use this for initialization
        void Start()
        {
            rigidBody2DRef = GetComponent<Rigidbody2D>();
            rigidBody2DRef.isKinematic = true;

            transform.position = new Vector2(Mathf.FloorToInt(transform.position.x) + 0.5f, Mathf.FloorToInt(transform.position.y) + 0.5f);
            currentPos = transform.position;
            UpdateTarget();
        }


        private void Update()
        {
            if (Vector2.Distance(targetPos, transform.position) < speedPerFrame * Time.deltaTime)
            {

                transform.position = targetPos;
                UpdateTarget();
            }
            else
            {

                velocity = (targetPos - currentPos).normalized * speedPerFrame;
                transform.position += (Vector3)velocity * Time.deltaTime;
                //Debug.Log(rigidBody2DRef.velocity);
            }
        }

        private void UpdateTarget()
        {
            int x = 0;
            int y = 0;
            x += Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
            x += Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
            y += Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
            y += Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

            currentPos = new Vector2(Mathf.FloorToInt(transform.position.x) + 0.5f, Mathf.FloorToInt(transform.position.y) + 0.5f);
            targetPos = currentPos + new Vector2(x, y);
            WalkableTestX = Physics2D.Raycast(currentPos, new Vector2(x, 0), 1.0f);
            if (WalkableTestX)
            {
                if (Mathf.Abs(WalkableTestX.point.x - currentPos.x) > 0.25f)
                {
                    x = 0;
                }
            }
            WalkableTestY = Physics2D.Raycast(currentPos, new Vector2(0, y), 1.0f);
            if (WalkableTestY)
            {
                if (Mathf.Abs(WalkableTestY.point.y - currentPos.y) > 0.25f)
                {
                    y = 0;
                }
            }
            if (x != 0 && y != 0)
            {
                WalkableTestT = Physics2D.Raycast(currentPos, targetPos - currentPos, 1.0f);
                if (WalkableTestT)
                {
                    if (Mathf.Abs(WalkableTestT.point.x - currentPos.x) > 0.25f)
                    {
                        x = 0;
                    }
                    if (Mathf.Abs(WalkableTestT.point.y - currentPos.y) > 0.25f)
                    {
                        x = 0;
                    }
                }
            }
            if (x != 0 || y != 0)
            {
                if (x != 0)
                    transform.localScale = new Vector3(-x, 1, 1);
            }
            targetPos = currentPos + new Vector2(x, y);
        }
        RaycastHit2D WalkableTestT;
        RaycastHit2D WalkableTestX;
        RaycastHit2D WalkableTestY;
        private void OnDrawGizmos()
        {
            Gizmos.DrawLine(currentPos, targetPos);
            if (WalkableTestX)
            {
                Gizmos.DrawSphere(WalkableTestX.point, 0.5f);
            }
            if (WalkableTestY)
            {
                Gizmos.DrawSphere(WalkableTestY.point, 0.25f);
            }
        }
    }
}
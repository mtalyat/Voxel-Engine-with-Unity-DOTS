using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class InputMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float ud = 0f;
        if(Input.GetKey(KeyCode.Space))
        {
            //move up
            ud += 1.0f;
        }
        if(Input.GetKey(KeyCode.LeftControl))
        {
            //move down
            ud -= 1.0f;
        }

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        
        Entities.ForEach((ref MovementInputComponent movement) => {
            movement.Forward = v;
            movement.Right = h;
            movement.Up = ud;
        }).ScheduleParallel();

        Entities.ForEach((ref SprintComponent sprint) =>
        {
            sprint.Value = isSprinting;
        }).ScheduleParallel();
    }
}

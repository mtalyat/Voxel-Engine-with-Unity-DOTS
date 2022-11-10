using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ColumnLoaderSystem : SystemBase
{
    private EndInitializationEntityCommandBufferSystem _bufferSystem;

    private EntityArchetype _columnArchetype;

    protected override void OnCreate()
    {
        _bufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

        _columnArchetype = EntityManager.CreateArchetype(typeof(ColumnTag), typeof(ColumnGenerateComponent));
    }

    protected override void OnUpdate()
    {
        var buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter();

        var columnArchetype = _columnArchetype;

        NativeList<int2> loadedIndices = new NativeList<int2>(100, Allocator.TempJob);

        //first, get the loaded chunk indices based on the chunk loader's position (should be the Player)
        Entities.ForEach((ref ColumnLoaderComponent loader, in Translation translation) =>
        {
            //update current column index
            loader.ColumnIndex = (int2)math.floor(translation.Value.xz / Constants.ChunkSize.xz);

            //get the indices of the columns that should be loaded
            GetIndicesInDistance(loader.ColumnIndex, loadedIndices, loader.Distance);
        }).Schedule();

        //second, remove any indices of chunks that we want to keep loaded
        //otherwise if the column we are checking is not in the list, destroy it
        Entities
            .WithAll<ColumnTag>()
            .WithNone<DestroyTag>()
            .ForEach((int entityInQueryIndex, Entity e, in ColumnIndexComponent columnIndex, in DynamicBuffer<LinkedEntityGroup> linkedEntities) =>
            {
                //remove the chunks from the list that we want to keep and are already loaded
                int2 colIndex = columnIndex.Value;
                int2 index;

                //go backwards, as we just move the elements to the end of the list instead of removing them
                //this is more performant
                for (int i = loadedIndices.Length - 1; i >= 0; i--)
                {
                    index = loadedIndices[i];
                    if(index.x == colIndex.x && index.y == colIndex.y)
                    {
                        //we found the column in the list, so remove it and be done
                        loadedIndices.RemoveAtSwapBack(i);
                        return;
                    }
                }

                //UnityEngine.Debug.Log(colIndex.ToString());

                //the column was not in the list, so that means it is now out of the view distance
                //destroy linked entities ("children") first, then the actual column entity
                for (int i = 0; i < linkedEntities.Length; i++)
                {
                    //buffer.DestroyEntity(entityInQueryIndex, linkedEntities[i].Value);
                    buffer.AddComponent<DestroyTag>(entityInQueryIndex, linkedEntities[i].Value);
                }
                //buffer.DestroyEntity(entityInQueryIndex, e);
                buffer.AddComponent<DestroyTag>(entityInQueryIndex, e);
            }).Schedule();

        //now load the remaining indices, as they are within the view distance and are not loaded
        Dependency = new LoadUnloadedColumnsJob
        {
            columnArchetype = columnArchetype,
            loadedIndices = loadedIndices.AsDeferredJobArray(),
            buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter()
        }.Schedule(loadedIndices, 64, Dependency);

        //dispose the list once the job is complete
        loadedIndices.Dispose(Dependency);

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    /// <summary>
    /// Add any column indices that should be loaded, based on the center and the view distance.
    /// </summary>
    /// <param name="center"></param>
    /// <param name="indices"></param>
    /// <param name="distance"></param>
    static void GetIndicesInDistance(int2 center, NativeList<int2> indices, int distance)
    {
        int2 i;
        for (int x = -distance; x < distance; x++)
        {
            for (int z = -distance; z < distance; z++)
            {
                i = center + new int2(x, z);

                //only add if it does not already exist
                if(!indices.Contains(i))
                {
                    indices.Add(i);
                }
            }
        }
    }

    struct LoadUnloadedColumnsJob : IJobParallelForDefer
    {
        //some unsafe stuff here to get the thread (entity) index
        //this is needed so we can use a parallel writer within a parallel job
        [NativeSetThreadIndex]
#pragma warning disable 0649
        int m_ThreadIndex;
#pragma warning restore 0649

        [ReadOnly]
        public NativeArray<int2> loadedIndices;

        public EntityCommandBuffer.ParallelWriter buffer;
        public EntityArchetype columnArchetype;

        public void Execute(int index)
        {
            var columnEntity = buffer.CreateEntity(m_ThreadIndex, columnArchetype);

            buffer.SetComponent(m_ThreadIndex, columnEntity, new ColumnGenerateComponent
            {
                Index = loadedIndices[index]
            });

            //buffer.AddComponent(m_ThreadIndex, columnEntity,);
        }
    }
}

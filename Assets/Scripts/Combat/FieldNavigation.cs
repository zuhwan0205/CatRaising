using System;
using System.Collections.Generic;
using UnityEngine;

// 정적인 원형 발자국과 작은 격자 경로망. 물리/내비메시 베이크 없이 런타임 필드를 지원합니다.
public sealed class FieldNavigation
{
    private struct Obstacle { public Vector3 center; public float radius; }
    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private const int Side = 37;
    private const float Half = 18, BodyRadius = .42f;
    private readonly Vector3[] nodes = new Vector3[Side*Side];
    private readonly List<int>[] links = new List<int>[Side*Side];
    private readonly Dictionary<int,int[]> distances = new Dictionary<int,int[]>();

    public void Add(Vector3 center, float radius) { obstacles.Add(new Obstacle {center=center,radius=radius}); }

    public bool IsFree(Vector3 point)
    {
        if (Math.Abs(point.x)>Half || Math.Abs(point.z)>Half) return false;
        foreach(var obstacle in obstacles)
        {
            float dx=point.x-obstacle.center.x, dz=point.z-obstacle.center.z;
            float radius=obstacle.radius+BodyRadius;
            if(dx*dx+dz*dz<radius*radius) return false;
        }
        return true;
    }

    public bool Clear(Vector3 from, Vector3 to)
    {
        if(!IsFree(from)||!IsFree(to)) return false;
        Vector3 line=to-from; line.y=0;
        float length=line.sqrMagnitude;
        foreach(var obstacle in obstacles)
        {
            Vector3 offset=obstacle.center-from; offset.y=0;
            float t=length<=.00001f?0:Mathf.Clamp01(Vector3.Dot(offset,line)/length);
            Vector3 delta=from+line*t-obstacle.center; delta.y=0;
            float radius=obstacle.radius+BodyRadius;
            if(delta.sqrMagnitude<radius*radius) return false;
        }
        return true;
    }

    public void Bake()
    {
        distances.Clear();
        for(int z=0;z<Side;z++) for(int x=0;x<Side;x++)
        {
            int i=z*Side+x;
            nodes[i]=new Vector3(x-Half,0,z-Half);
            links[i]=new List<int>(4);
        }
        for(int z=0;z<Side;z++) for(int x=0;x<Side;x++)
        {
            int i=z*Side+x;
            if(!IsFree(nodes[i])) continue;
            if(x+1<Side) Connect(i,i+1);
            if(z+1<Side) Connect(i,i+Side);
        }
    }

    private void Connect(int a,int b)
    {
        if(!Clear(nodes[a],nodes[b])) return;
        links[a].Add(b); links[b].Add(a);
    }

    public Vector3 Waypoint(Vector3 from,Vector3 goal)
    {
        if(Clear(from,goal)) return goal;
        int end=-1;float best=float.MaxValue;
        for(int i=0;i<nodes.Length;i++)
        {
            float d=(nodes[i]-goal).sqrMagnitude;
            if(links[i].Count==0||d>=best||!Clear(nodes[i],goal))continue;
            best=d;end=i;
        }
        if(end<0)return from;
        if(!distances.TryGetValue(end,out var costs))
        {
            costs=new int[nodes.Length];for(int i=0;i<costs.Length;i++)costs[i]=-1;
            var queue=new Queue<int>();queue.Enqueue(end);costs[end]=0;
            while(queue.Count>0)
            {
                int i=queue.Dequeue();
                foreach(int next in links[i]) if(costs[next]<0) {costs[next]=costs[i]+1;queue.Enqueue(next);}
            }
            if(distances.Count>=8)distances.Clear();
            distances[end]=costs;
        }
        int anchor=-1;best=float.MaxValue;
        for(int i=0;i<nodes.Length;i++)
        {
            float d=(nodes[i]-from).sqrMagnitude;
            if(costs[i]<0||d>=best||!Clear(from,nodes[i]))continue;
            best=d;anchor=i;
        }
        if(anchor<0)return from;
        // 현재 위치에서 볼 수 있는 경로의 가장 먼 지점까지 곧바로 이동합니다.
        int step=anchor;
        while(costs[step]>0)
        {
            int next=-1;
            foreach(int candidate in links[step])
                if(costs[candidate]==costs[step]-1) {next=candidate;break;}
            if(next<0||!Clear(from,nodes[next]))break;
            step=next;
        }
        return nodes[step];
    }

    public Vector3 Move(Vector3 from,Vector3 delta)
    {
        delta.y=0;
        Vector3 to=from+delta;
        to.x=Mathf.Clamp(to.x,-Half,Half);to.z=Mathf.Clamp(to.z,-Half,Half);
        if(Clear(from,to))return to;
        Vector3 alongX=new Vector3(to.x,0,from.z);
        if(Clear(from,alongX))from=alongX;
        Vector3 alongZ=new Vector3(from.x,0,to.z);
        return Clear(from,alongZ)?alongZ:from;
    }
}

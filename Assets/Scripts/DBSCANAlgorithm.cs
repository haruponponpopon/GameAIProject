using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;

class Point
{
    public const int NOISE = 100;
    public const int UNCLASSIFIED = 0;
    public float X, Y, Z, ClusterId;
    public Point(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
    public static float DistanceSquared(Point p1, Point p2)
    {
        float diffX = p2.X - p1.X;
        float diffY = p2.Y - p1.Y;
        float diffZ = p2.Z - p1.Z;
        return diffX * diffX + diffY * diffY + diffZ * diffZ;
    }
}
public class DBSCANAlgorithm : MonoBehaviour
{
    Boid[] boids;
    public double eps = 1.8;//小さくするほどクラスターが多くなる
    public int minPts = 3;//最小のグループ
    GameObject clusterNum;
    double totalTime = 0;
    int timeFactorNum = 0;
    // int clusterCount = 0;
    void Start()
    {
        //Textの処理
        this.clusterNum = GameObject.Find("Cluster_num");

        List<Point> points = new List<Point>();//3次元座標のリスト
        boids = FindObjectsOfType<Boid> ();
        foreach (Boid b in boids) {
            points.Add(new Point(b.position[0], b.position[1], b.position[2]));
        }
        List<List<int>> clusters = GetClusters(points, eps, minPts);//特定のクラスターの属するidを返す
        int total = 0;
        // Debug.Log("Cluster count is " + clusters.Count + "\n");
        // Debug.Log(clusters);
        for (int i = 0; i < clusters.Count; i++)
        {
            int count = clusters[i].Count;
            total += count;
            // Debug.Log("Cluster" + i);
            foreach (int p in clusters[i]) {//pはboidsの何番目か
                // Debug.Log(p);
                boids[p].type = i;
            }
        }
    }
    void Update()
    {
        //時間の計測
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start ();
        boids = FindObjectsOfType<Boid> ();

        List<Point> points = new List<Point>();
        foreach (Boid b in boids) {
            points.Add(new Point(b.position[0], b.position[1], b.position[2]));
        }
        List<List<int>> clusters = GetClusters(points, eps, minPts);
        int cluster_count = 0;
        for (int i = 0; i < clusters.Count; i++)
        {
            if (i<90&&clusters[i].Count>0)cluster_count++;
            foreach (int p in clusters[i]) {
                boids[p].type = i;
            }
        }
        //Textの更新
        // this.clusterNum.GetComponent<Text>().text = "cluster: " + cluster_count.ToString();
        sw.Stop(); //計測終了
        totalTime += sw.Elapsed.TotalMilliseconds;
        timeFactorNum++;
        // clusterCount += cluster_count;
        if (timeFactorNum==1000){
            Debug.Log("total time: " + totalTime.ToString());
            // Debug.Log("count: " + timeFactorNum.ToString());
            // Debug.Log("cluster count: " + clusterCount.ToString());
            totalTime = 0;
            timeFactorNum = 0;
            // clusterCount = 0;
        }
    }
    static List<List<int>> GetClusters(List<Point> points, double eps, int minPts)
    {
        if (points == null) return null;
        List<List<int>> clusters = new List<List<int>>();
        eps *= eps; // square eps
        int clusterId = 1;
        for (int i = 0; i < points.Count; i++)
        {
            Point p = points[i];
            if (p.ClusterId == Point.UNCLASSIFIED)
            {
                if (ExpandCluster(points, p, clusterId, eps, minPts)) clusterId++;
            }
        }
        // sort out points into their clusters, if any
        int maxClusterId =  (int)points.OrderBy(p => p.ClusterId).Last().ClusterId;
        if (maxClusterId < 1) return clusters; // no clusters, so list is empty
        for (int i = 0; i < maxClusterId; i++) clusters.Add(new List<int>());
        for(int i = 0; i < points.Count; i++)
        {
            Point p = points[i];
            if (p.ClusterId > 0) clusters[(int)p.ClusterId - 1].Add(i);
        }
        return clusters;
    }
    static List<Point> GetRegion(List<Point> points, Point p, double eps)
    {
        List<Point> region = new List<Point>();
        for (int i = 0; i < points.Count; i++)
        {
            float distSquared = Point.DistanceSquared(p, points[i]);
            if (distSquared <= eps) region.Add(points[i]);
        }
        return region;
    }
    static bool ExpandCluster(List<Point> points, Point p, int clusterId, double eps, int minPts)
    {
        List<Point> seeds = GetRegion(points, p, eps);
        if (seeds.Count < minPts) // no core point
        {
            p.ClusterId = Point.NOISE;
            return false;
        }
        else // all points in seeds are density reachable from point 'p'
        {
            for (int i = 0; i < seeds.Count; i++) seeds[i].ClusterId = clusterId;
            seeds.Remove(p);
            while (seeds.Count > 0)
            {
                Point currentP = seeds[0];
                List<Point> result = GetRegion(points, currentP, eps);
                if (result.Count >= minPts)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        Point resultP = result[i];
                        if (resultP.ClusterId == Point.UNCLASSIFIED || resultP.ClusterId == Point.NOISE)
                        {
                            if (resultP.ClusterId == Point.UNCLASSIFIED) seeds.Add(resultP);
                            resultP.ClusterId = clusterId;
                        }
                    }
                }
                seeds.Remove(currentP);
            }
            return true;
        }
    }
}

// public class DBSCANAlgorithm : MonoBehaviour
// {
//     Boid[] boids;
//     public double len = 50;//小さくするほどクラスターが多くなる
//     double totalTime = 0;
//     int timeFactorNum = 0;
//     // int clusterCount = 0;
//     void Start()
//     {
//         boids = FindObjectsOfType<Boid> ();
//         foreach (Boid b in boids) {
//             int x = (int)((b.position[0]+25)/len);
//             int y = (int)(b.position[1]/len);
//             int z = (int)((b.position[2]+25)/len);
//             b.type = x*10000+y*100+z;
//             // Debug.Log("x: " + x.ToString() + "x: " + b.position[0].ToString());
//             // Debug.Log("y: " + y.ToString() + "y: " + b.position[1].ToString());
//             // Debug.Log("z: " + z.ToString() + "z: " + b.position[2].ToString());
//             // Debug.Log(b.type);
//         }
//     }
//     void Update()
//     {
//         //時間の計測
//         var sw = new System.Diagnostics.Stopwatch();
//         sw.Start ();
//         boids = FindObjectsOfType<Boid> ();
//         foreach (Boid b in boids) {
//             int x = (int)((b.position[0]+25)/len);
//             int y = (int)(b.position[1]/len);
//             int z = (int)((b.position[2]+25)/len);
//             // Debug.Log("x: " + x.ToString());
//             // Debug.Log("y: " + y.ToString());
//             // Debug.Log("z: " + z.ToString());
//             b.type = x*10000+y*100+z;
//         }
//         //Textの更新
//         // this.clusterNum.GetComponent<Text>().text = "cluster: " + cluster_count.ToString();
//         sw.Stop(); //計測終了
//         totalTime += sw.Elapsed.TotalMilliseconds;
//         timeFactorNum++;
//         // clusterCount += cluster_count;
//         if (timeFactorNum==1000){
//             Debug.Log("total time: " + totalTime.ToString());
//             // Debug.Log("count: " + timeFactorNum.ToString());
//             // Debug.Log("cluster count: " + clusterCount.ToString());
//             totalTime = 0;
//             timeFactorNum = 0;
//             // clusterCount = 0;
//         }
//     }
// }
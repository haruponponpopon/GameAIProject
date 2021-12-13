/*
Copyright (c) 2019 Sebastian Lague
Released under the MIT license
https://github.com/SebLague/Boids/blob/master/LICENSE
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {

    const int threadGroupSize = 1024;

    public EnemySettings settings;
    public ComputeShader compute;
    Enemy[] enemies;
    Boid[] boids;

    void Start () {
        enemies = FindObjectsOfType<Enemy> ();
        boids = FindObjectsOfType<Boid> ();
        foreach (Enemy e in enemies) {
            int type;
            type = 0;
            e.Initialize (settings,type);
        }

    }

    void Update () {
        if (enemies != null) {

            int numEnemies = enemies.Length;
            var enemyData = new EnemyData[numEnemies];
            int numBoids = boids.Length;
            var boidData = new BoidData[numBoids];

            for (int i = 0; i < enemies.Length; i++) {      //compute shader用のデータを格納
                enemyData[i].position = enemies[i].position;
                enemyData[i].direction = enemies[i].forward;
                // enemyData[i].type=enemies[i].type;
            }
            for (int i=0; i < boids.Length; i++) {
                boidData[i].position = boids[i].position;
            }

            var enemyBuffer = new ComputeBuffer (numEnemies, EnemyData.Size);
            enemyBuffer.SetData (enemyData);
            var boidBuffer = new ComputeBuffer (numBoids, BoidData.Size);
            boidBuffer.SetData (boidData);

            compute.SetBuffer (0, "enemies", enemyBuffer);
            compute.SetBuffer (0, "boids", boidBuffer);
            compute.SetInt ("numEnemies", enemies.Length);
            compute.SetInt ("numBoids", boids.Length);
            compute.SetFloat ("viewRadius", settings.perceptionRadius);
            compute.SetFloat ("viewBoidRadius", settings.detectboidRange);
            compute.SetFloat ("avoidRadius", settings.avoidanceRadius);

            int threadGroups = Mathf.CeilToInt (numBoids / (float) threadGroupSize);
            compute.Dispatch (0, threadGroups, 1, 1);     //コンピュートシェーダーを実行

            enemyBuffer.GetData (enemyData);
            for (int i = 0; i < enemies.Length; i++) {                
                enemies[i].centreOfBoids = enemyData[i].boidCentre;
                enemies[i].numPerceivedBoids = enemyData[i].numboids;
                enemies[i].UpdateEnemy ();
            }
            boidBuffer.Release ();
            enemyBuffer.Release ();
        }
    }

    public struct EnemyData {
        public Vector3 position;
        public Vector3 direction;

        public Vector3 boidCentre;
        public int numboids;

        public static int Size {
            get {
                return sizeof (float) * 3 * 3 + sizeof (int)*1;
            }
        }
    }
    public struct BoidData {
        public Vector3 position;
        // public Vector3 direction;
        // public int type;

        // public Vector3 flockHeading;
        // public Vector3 flockCentre;
        // public Vector3 avoidanceHeading;
        // public int numFlockmates;

        public static int Size {
            get {
                return sizeof (float) * 3 * 1;
            }
        }
    }
}
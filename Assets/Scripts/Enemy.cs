/*
Copyright (c) 2019 Sebastian Lague
Released under the MIT license
https://github.com/SebLague/Boids/blob/master/LICENSE
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    EnemySettings settings;

    // State
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward;
    Vector3 velocity;
    public int type;

    // To update:
    Vector3 acceleration;
    [HideInInspector]
    public Vector3 centreOfBoids;
    [HideInInspector]
    public int numPerceivedBoids;

    // Cached
    Material material;
    public Material enemyMat;

    Transform cachedTransform;         //transformへのアクセスは重いのでキャッシュする

    void Awake () {
        cachedTransform = transform;
    }

    public void Initialize (EnemySettings settings,int type) {
        this.settings = settings;
        this.type=type;
        transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material=enemyMat;
        
        position = cachedTransform.position;
        forward = cachedTransform.forward;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;
    }

    public void UpdateEnemy () {
        Vector3 acceleration = Vector3.zero;

        if (numPerceivedBoids != 0) {
            centreOfBoids /= numPerceivedBoids;               //自分の周りにいるboidの重心を求める

            Vector3 offsetToBoidsCentre = (centreOfBoids - position);      //重心へのベクトル

            var cohesionForce = SteerTowards (offsetToBoidsCentre) * settings.cohesionWeight;  //近くの魚の重心へ向かう力

            acceleration += cohesionForce;
        }

        if (IsHeadingForCollision ()) {
            Vector3 collisionAvoidDir = ObstacleRays ();         //障害物を避ける方向を取得
            Vector3 collisionAvoidForce = SteerTowards (collisionAvoidDir) * settings.avoidCollisionWeight;   //障害物を避ける力
            acceleration += collisionAvoidForce;
        }

        velocity += acceleration * Time.deltaTime;        //加速度を用いて速度を変更する。
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);      //速度のスカラが範囲内に収まるようにする
        velocity = dir * speed;

        cachedTransform.position += velocity * Time.deltaTime;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;
        //はみ出たenemyを中心に持っていく
        if (position.x < -25 || position.x > 25 || position.y < 0 || position.y > 10 || position.z < -25 || position.z > 25){
            Debug.Log("enemy initialized\n");
            type = 10;
            var pos = new Vector3(0, 5, 0);
            position = pos;
            cachedTransform.position = pos;
        }
    }

    bool IsHeadingForCollision () {         //障害物が進む先にあるかどうかを判定
        RaycastHit hit;
        if (Physics.SphereCast (position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask)) {
            return true;
        } else { }
        return false;
    }

    Vector3 ObstacleRays () {                                //障害物がない方向ベクトルを取得
        Vector3[] rayDirections = BoidHelper.directions;     //ここに方向ベクトルの候補が格納される

        for (int i = 0; i < rayDirections.Length; i++) {
            Vector3 dir = cachedTransform.TransformDirection (rayDirections[i]);
            Ray ray = new Ray (position, dir);
            if (!Physics.SphereCast (ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask)) {
                return dir;          //rayの先に障害物がなかったらその方向を返す。
            }
        }

        return forward;
    }

    Vector3 SteerTowards (Vector3 vector) {                             //力が大きくなりすぎないように上から抑える
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude (v, settings.maxSteerForce);
    }

}
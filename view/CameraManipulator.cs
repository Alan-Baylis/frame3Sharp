﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    public struct CameraState
    {
        public Vector3f camPosition;
        public Quaternionf camRotation;
        public Vector3f scenePosition;
        public Quaternionf sceneRotation;
        public Vector3f target;
        public float turntableAzimuth, turntableAltitude;
    }


    // [TODO] this should just know about current scene, then we can
    //   get rid of all the goddamn arguments!!


    public class CameraManipulator : MonoBehaviour
    {
        Camera getCamera()
        {
            return gameObject.GetComponent<Camera>();
        }

        // camera stuff
        public float turntableAzimuth = 0;
        public float turntableAltitude = 0;

        public CameraManipulator()
        {
        }



        public Frame3f SceneGetFrame(FScene scene)
        {
            return scene.RootGameObject.GetWorldFrame();
        }
        public void SceneSetFrame(FScene scene, Frame3f frame)
        {
            scene.RootGameObject.SetPosition( frame.Origin );
            scene.RootGameObject.SetRotation( frame.Rotation );
        }


        public void SceneRotateAround(FScene scene, Quaternionf rotation, Vector3f position)
        {
            Frame3f curF = scene.RootGameObject.GetWorldFrame();

            curF.RotateAround(position, rotation);

            scene.RootGameObject.SetPosition( curF.Origin );
            scene.RootGameObject.SetRotation( curF.Rotation );
        }


        public void SceneTumble(FScene scene, Camera cam, float dx, float dy)
        {
            Vector3 up = cam.gameObject.transform.up;
            Vector3 right = cam.gameObject.transform.right;

            //Vector3 curOrigin = scene.RootGameObject.transform.position;
            Vector3 curOrigin = cam.GetTarget();

            scene.RootGameObject.RotateAroundD(curOrigin, up, dx);
            scene.RootGameObject.RotateAroundD(curOrigin, right, dy);
        }


        public void SceneTumbleAround(FScene scene, Vector3f position, float dx, float dy)
        {
            Vector3 targetPos = getCamera().GetTarget();
            getCamera().SetTarget(position);
            SceneTumble(scene, getCamera(), dx, dy);
            getCamera().SetTarget(targetPos);
        }


        public void SceneOrbit(FScene scene, Camera cam, float deltaAzimuth, float deltaAltitude, bool bSet = false)
        {
            //Vector3 sceneOrigin = scene.RootGameObject.transform.position;
            Vector3 rotTarget = cam.GetTarget();

            // [RMS] Orbiting around the Target point is kind of a weird concept when
            //   you are moving the scene and not the camera. Basically we want to rotate
            //   the scene around the target, but not incrementally - based on a pair of
            //   spherical angles. So, we have to return to a 'reference' state each update.
            //   Simplest way to do that is to "un-rotate" by the angles before the delta...
            //   (???)

            Vector3f up = Vector3f.AxisY;
            Vector3f right = cam.transform.right;

            scene.RootGameObject.RotateAroundD(rotTarget, right, -turntableAltitude);
            scene.RootGameObject.RotateAroundD(rotTarget, up, -turntableAzimuth);

            if (bSet) {
                turntableAzimuth = deltaAzimuth;
                turntableAltitude = deltaAltitude;
            } else {
                turntableAzimuth -= deltaAzimuth;
                turntableAltitude += deltaAltitude;
            }
            turntableAltitude = Mathf.Clamp(turntableAltitude, -89.9f, 89.9f);

            scene.RootGameObject.RotateAroundD(rotTarget, up, turntableAzimuth);
            scene.RootGameObject.RotateAroundD(rotTarget, right, turntableAltitude);
        }


        public void ResetSceneOrbit(FScene scene, bool bAzimuth, bool bAltitude, bool bApply = true)
        {
            Vector3f up = Vector3f.AxisY;
            Vector3f right = getCamera().transform.right;

            Vector3 rotTarget = getCamera().GetTarget();
            if ( bApply ) {
                scene.RootGameObject.RotateAroundD(rotTarget, right, -turntableAltitude);
                scene.RootGameObject.RotateAroundD(rotTarget, up, -turntableAzimuth);
            }
            if (bAzimuth == true)
                turntableAzimuth = 0.0f;
            if (bAltitude == true)
                turntableAltitude = 0.0f;
            if ( bApply ) {
                scene.RootGameObject.RotateAroundD(rotTarget, up, turntableAzimuth);
                scene.RootGameObject.RotateAroundD(rotTarget, right, turntableAltitude);
            }
        }



        public void SceneOrbitAround(FScene scene, Vector3f position, float deltaAzimuth, float deltaAltitude)
        {
            Vector3 targetPos = getCamera().GetTarget();
            getCamera().SetTarget(position);
            SceneOrbit(scene, getCamera(), deltaAzimuth, deltaAltitude);
            getCamera().SetTarget(targetPos);
        }



        public void ResetScenePosition(FScene scene)
        {
            scene.RootGameObject.SetPosition(Vector3f.Zero);
            Vector3 targetPos = Vector3.zero;
            getCamera().SetTarget(targetPos);
        }


        public void SceneTranslate(FScene scene, Vector3f translate, bool bFrameAxes )
        {
            scene.RootGameObject.Translate( translate, bFrameAxes );
        }

        public void ScenePan(FScene scene, fCamera cam, float dx, float dy)
        {
            float fScale = scene.GetSceneScale();

            Frame3f camFrame = cam.GetWorldFrame();
            Vector3f right = camFrame.X;
            Vector3f up = camFrame.Y;
            Vector3f delta = dx * fScale * right + dy * fScale * up;
            Vector3f newPos = scene.RootGameObject.GetPosition() + delta;
            scene.RootGameObject.SetPosition( newPos );
        }

        public void SceneZoom(FScene scene, fCamera cam, float dz)
        {
            if (dz == 0.0f)
                return;

            float fScale = scene.GetSceneScale();

            //Vector3f fw = cam.gameObject.transform.forward;
            Vector3f fw = cam.GetTarget() - cam.GetPosition();
            float fTargetDist = fw.Length;
            fw.Normalize();

            float fMinTargetDist = 0.1f;
            if (dz > 0 && fTargetDist - dz < fMinTargetDist)
                dz = fTargetDist - fMinTargetDist;

            Vector3f delta = dz * fScale * fw;
            scene.RootGameObject.Translate(-delta, false);
            cam.SetTarget(cam.GetTarget() - delta);
        }


        public void ScenePanFocus(FScene scene, fCamera camera, Vector3f focusPoint, bool bAnimated)
        {
            CameraAnimator animator = camera.Animator();
            if (bAnimated == false || animator == null) {
                // figure out the pan that we would apply to camera, then apply the delta to the scene
                Vector3f curPos = camera.GetPosition();
                Vector3f curDir = camera.GetWorldFrame().Z;
                float fDist = Vector3.Dot((focusPoint - curPos), curDir);
                Vector3f newPos = focusPoint - fDist * curDir;
                Vector3f delta = curPos - newPos;

                scene.RootGameObject.Translate(delta, false);
                camera.SetTarget(focusPoint);
                           
            } else
                animator.PanFocus(focusPoint);
        }





        public CameraState GetCurrentState(FScene scene)
        {
            CameraState s = new CameraState();
            s.camPosition = getCamera().transform.position;
            s.camRotation = getCamera().transform.rotation;
            s.scenePosition = scene.RootGameObject.GetPosition();
            s.sceneRotation = scene.RootGameObject.GetRotation();
            s.target = getCamera().GetTarget();
            s.turntableAzimuth = turntableAzimuth;
            s.turntableAltitude = turntableAltitude;
            return s;
        }

        // restores scene values & cam target (but not cam position)
        public void SetCurrentSceneState(FScene scene, CameraState s)
        {
            scene.RootGameObject.SetPosition(s.scenePosition);
            scene.RootGameObject.SetRotation(s.sceneRotation);
            getCamera().SetTarget(s.target);
            turntableAzimuth = s.turntableAzimuth;
            turntableAltitude = s.turntableAltitude;
        }



        public class RateControlInfo
        {
            public float lastTime;
            public float maxSpeed;
            public float maxAccel;
            public float angleMultipler;
            public float curSpeedX, curSpeedY;
            public Vector2f startPos;
            public float rampUpRadius;
            public float deadZoneRadius;
            public bool StayLevel;
            public RateControlInfo(Vector2 startPos)
            {
                lastTime = FPlatform.RealTime();
                maxSpeed = 5.0f;
                maxAccel = 10.5f;
                angleMultipler = 4.0f;
                curSpeedX = curSpeedY = 0.0f;
                rampUpRadius = 10.0f;
                deadZoneRadius = 1.0f;
                StayLevel = true;
                this.startPos = startPos;
            }
        }


        float rc_update_speed(float delta_x, float rampUpRadius, float deadZoneRadius, float maxAccel, float maxSpeed, float dt, float curSpeed)
        {
            float ty = g3.MathUtil.LinearRampT(rampUpRadius, deadZoneRadius, delta_x);
            float target_vel = ty * maxSpeed;

            // this doesn't make any sense!
            float accel = g3.MathUtil.RangeClamp((target_vel - curSpeed), maxAccel);

            return g3.MathUtil.RangeClamp(curSpeed + dt * accel, target_vel);
        }



        public void SceneRateControlledFly(FScene scene, fCamera cam, Vector2f curPos, RateControlInfo rc )
        {
            float dt = (FPlatform.RealTime() - rc.lastTime);
            rc.lastTime = FPlatform.RealTime();

            float delta_y = (curPos.y - rc.startPos.y);
            rc.curSpeedY =
                rc_update_speed(delta_y, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedY);
            float delta_x = (curPos.x - rc.startPos.x);
            rc.curSpeedX =
                rc_update_speed(delta_x, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedX);

            Frame3f camFrame = cam.GetWorldFrame();
            Vector3f forward = camFrame.Z;
            Vector3f up = camFrame.Y;
            if (rc.StayLevel) {
                forward = new Vector3(forward.x, 0.0f, forward.z);
                forward.Normalize();
                up = Vector3.up;
            }
            Vector3f curScenePos = scene.RootGameObject.GetPosition();
            Vector3f newScenePos = curScenePos - dt * rc.curSpeedY * forward;
            scene.RootGameObject.SetPosition(newScenePos);
            scene.RootGameObject.RotateAroundD(
                camFrame.Origin, up, -dt * rc.curSpeedX * rc.angleMultipler);
        }



        public void SceneRateControlledZoom(FScene scene, fCamera cam, Vector2f curPos, RateControlInfo rc)
        {
            float dt = (FPlatform.RealTime() - rc.lastTime);
            rc.lastTime = FPlatform.RealTime();

            float delta_y = (curPos.y - rc.startPos.y);
            rc.curSpeedY =
                rc_update_speed(delta_y, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedY);
            float delta_x = (curPos.x - rc.startPos.x);
            rc.curSpeedX =
                rc_update_speed(delta_x, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedX);


            Frame3f camFrame = cam.GetWorldFrame();
            Vector3f forward = camFrame.Z;
            Vector3f up = camFrame.Y;
            if (rc.StayLevel) {
                forward = new Vector3(forward.x, 0.0f, forward.z);
                forward.Normalize();
                up = Vector3.up;
            }
            Vector3f right = Vector3.Cross(up, forward);
            Vector3f curScenePos = scene.RootGameObject.GetPosition();
            Vector3f newScenePos = curScenePos - dt * (rc.curSpeedY * forward + rc.curSpeedX * right);
            scene.RootGameObject.SetPosition(newScenePos);
        }


        // in egocentric camera system the "eye" is moving up/down, which means the scene moves in opposite direction
        public void SceneRateControlledEgogentricPan(FScene scene, fCamera cam, Vector2f curPos, RateControlInfo rc)
        {
            float dt = (FPlatform.RealTime() - rc.lastTime);
            rc.lastTime = FPlatform.RealTime();

            float delta_y = (curPos.y - rc.startPos.y);
            rc.curSpeedY =
                rc_update_speed(delta_y, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedY);
            float delta_x = (curPos.x - rc.startPos.x);
            rc.curSpeedX =
                rc_update_speed(delta_x, rc.rampUpRadius, rc.deadZoneRadius, rc.maxAccel, rc.maxSpeed, dt, rc.curSpeedX);


            Frame3f camFrame = cam.GetWorldFrame();
            Vector3f forward = camFrame.Z;
            Vector3f up = camFrame.Y;
            if (rc.StayLevel) {
                forward = new Vector3(forward.x, 0.0f, forward.z);
                forward.Normalize();
                up = Vector3.up;
            }
            Vector3f right = Vector3.Cross(up, forward);
            Vector3f curScenePos = scene.RootGameObject.GetPosition();
            Vector3f newScenePos = curScenePos - dt * (rc.curSpeedY * up + rc.curSpeedX * right);
            scene.RootGameObject.SetPosition(newScenePos);
        }




    }
}

﻿using OWML.Utils;
using PacificEngine.OW_CommonResources.Game.Display;
using PacificEngine.OW_CommonResources.Game.Resource;
using PacificEngine.OW_CommonResources.Geometry;
using PacificEngine.OW_CommonResources.Geometry.Orbits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PacificEngine.OW_CommonResources.Game.State
{
    public class ScaleState : IEquatable<ScaleState>
    {
        public static ScaleState identity { get { return new ScaleState(Vector3.one, Vector3.one); } }

        public Vector3 lossyScale { get; }
        public Vector3 localScale { get; }

        public ScaleState(Vector3 lossyScale, Vector3 localScale)
        {
            this.lossyScale = lossyScale;
            this.localScale = localScale;
        }

        public static ScaleState fromCurrentState(HeavenlyBody target)
        {
            return fromCurrentState(Position.getBody(target));
        }

        public static ScaleState fromCurrentState(OWRigidbody target)
        {
            if (target == null || target.GetRigidbody() == null)
            {
                return null;
            }

            var lossyScale = target?.transform?.lossyScale ?? identity.lossyScale;
            var localScale = target.GetLocalScale();

            return new ScaleState(lossyScale, localScale);
        }

        public static ScaleState fromCurrentState(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            return new ScaleState(target.lossyScale, target.localScale);
        }

        public static ScaleState fromCurrentState(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            var rigidBody = target.GetComponent<OWRigidbody>();
            var transform = target.transform;

            if (rigidBody != null)
            {
                return fromCurrentState(target);
            }
            else
            {
                return fromCurrentState(transform);
            }
        }

        public override string ToString()
        {
            var lossyScale = this.lossyScale == null ? "" : DisplayConsole.logVector(this.lossyScale);
            var localScale = this.localScale == null ? "" : DisplayConsole.logVector(this.localScale);
            return $"({lossyScale}, {localScale})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is ScaleState)
            {
                return Equals((ScaleState)(other as ScaleState));
            }
            return false;
        }

        public bool Equals(ScaleState other)
        {
            if (other != null)
            {
                return ((lossyScale == null && other.lossyScale == null) || lossyScale.Equals(other.lossyScale))
                    && ((localScale == null && other.localScale == null) || localScale.Equals(other.localScale));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (lossyScale.GetHashCode() * 4)
                + (localScale.GetHashCode() * 16);
        }
    }

    public class PositionState : IEquatable<PositionState>
    {
        public static PositionState offset { get {
                var position = Vector3.zero;
                var velocity = Vector3.zero;
                var acceleration = Vector3.zero;
                var jerk = Vector3.zero;
                try
                {
                    position = Locator.GetCenterOfTheUniverse().GetOffsetPosition();
                    velocity = Locator.GetCenterOfTheUniverse().GetOffsetVelocity();
                } catch (Exception) { }
                return new PositionState(position, velocity, acceleration, jerk); } }
        public static PositionState identity { get { return new PositionState(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero); } }

        public Vector3 position { get; }
        public Vector3 velocity { get; }
        public Vector3 acceleration { get; }
        public Vector3 jerk { get; }

        public PositionState(Vector3 position, Vector3 velocity, Vector3 acceleration, Vector3 jerk)
        {
            this.position = position;
            this.velocity = velocity;
            this.acceleration = acceleration;
            this.jerk = jerk;
        }

        private static Vector3 getStartVelocity(GameObject target)
        {
            var initialMotion = target.GetComponent<InitialMotion>();
            var initialVelocity = target.GetComponent<InitialVelocity>();
            var matchInitialMotion = target.GetComponent<MatchInitialMotion>();

            if (initialMotion != null)
            {
                return initialMotion.GetInitVelocity();
            }
            else if (initialVelocity != null)
            {
                return initialVelocity.GetValue<Vector3>("initVelocityDirection").normalized * initialVelocity.GetValue<float>("initVelocityMagnitude");
            }
            else if (matchInitialMotion != null)
            {
                return matchInitialMotion.CalculateMatchVelocity();
            }
            else
            {
                return Vector3.zero;
            }
        }

        public static PositionState fromCurrentState(HeavenlyBody target, bool useInitialVelocity = false)
        {
            return fromCurrentState(Position.getBody(target), useInitialVelocity);
        }

        public static PositionState fromCurrentState(OWRigidbody target, bool useInitialVelocity = false)
        {
            if (target == null || target.GetRigidbody() == null)
            {
                return null;
            }

            var offset = PositionState.offset;
            var position = target.GetPosition() - offset.position;
            var velocity = target.GetVelocity_Internal() - offset.velocity;
            var acceleration = target.GetAcceleration() - offset.acceleration;
            var jerk = target.GetJerk() - offset.jerk;

            if (useInitialVelocity)
            {
                velocity = getStartVelocity(target.gameObject);
                acceleration = Vector3.zero;
                jerk = Vector3.zero;
            }

            return new PositionState(position, velocity, acceleration, jerk);
        }

        private static PositionState fromCurrentState(KinematicRigidbody target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var offset = PositionState.offset;
            var position = target.IntegratePosition() - offset.position;
            var velocity = target.velocity - offset.velocity;
            var acceleration = Vector3.zero;
            var jerk = Vector3.zero;

            if (useInitialVelocity)
            {
                velocity = getStartVelocity(target.gameObject);
                acceleration = Vector3.zero;
                jerk = Vector3.zero;
            }

            return new PositionState(position, velocity, acceleration, jerk);
        }

        private static PositionState fromCurrentState(Rigidbody target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var offset = PositionState.offset;
            var position = target.position - offset.position;
            var velocity = target.velocity - offset.velocity;
            var acceleration = Vector3.zero;
            var jerk = Vector3.zero;

            if (useInitialVelocity)
            {
                velocity = getStartVelocity(target.gameObject);
                acceleration = Vector3.zero;
                jerk = Vector3.zero;
            }

            return new PositionState(position, velocity, acceleration, jerk);
        }

        public static PositionState fromCurrentState(Transform target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var offset = PositionState.offset;
            var position = target.position - offset.position;
            var velocity = Vector3.zero;
            var acceleration = Vector3.zero;
            var jerk = Vector3.zero;

            if (useInitialVelocity)
            {
                velocity = getStartVelocity(target.gameObject);
                acceleration = Vector3.zero;
                jerk = Vector3.zero;
            }

            return new PositionState(position, velocity, acceleration, jerk);
        }

        public static PositionState fromCurrentState(GameObject target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var owRigidBody = target.GetComponent<OWRigidbody>();
            var transform = target.transform;

            if (owRigidBody != null)
            {
                return fromCurrentState(owRigidBody, useInitialVelocity);
            }
            else
            {
                return fromCurrentState(transform, useInitialVelocity);
            }
        }

        public override string ToString()
        {
            var position = this.position == null ? "" : DisplayConsole.logVector(this.position);
            var velocity = this.velocity == null ? "" : DisplayConsole.logVector(this.velocity);
            var acceleration = this.acceleration == null ? "" : DisplayConsole.logVector(this.acceleration);
            var jerk = this.jerk == null ? "" : DisplayConsole.logVector(this.jerk);
            return $"({position}, {velocity}, {acceleration}, {jerk})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is PositionState)
            {
                return Equals((PositionState)(other as PositionState));
            }
            return false;
        }
        public bool Equals(PositionState other)
        {
            if (other != null)
            {
                return ((position == null && other.position == null) || position.Equals(other.position))
                    && ((velocity == null && other.velocity == null) || velocity.Equals(other.velocity))
                    && ((acceleration == null && other.acceleration == null) || acceleration.Equals(other.acceleration))
                    && ((jerk == null && other.jerk == null) || jerk.Equals(other.jerk));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (position.GetHashCode() * 4)
                + (velocity.GetHashCode() * 16)
                + (acceleration.GetHashCode() * 64)
                + (acceleration.GetHashCode() * 256);
        }
    }

    public class OrientationState : IEquatable<OrientationState>
    {
        public static OrientationState identity { get { return new OrientationState(Quaternion.identity, Vector3.zero, Vector3.zero); } }

        public Quaternion rotation { get; }
        public Vector3 angularVelocity { get; }
        public Vector3 angularAcceleration { get; }

        public Vector3 forward { get { return rotation * Vector3.forward; } }
        public Vector3 back { get { return rotation * Vector3.back; } }
        public Vector3 up { get { return rotation * Vector3.up; } }
        public Vector3 down { get { return rotation * Vector3.down; } }
        public Vector3 left { get { return rotation * Vector3.left; } }
        public Vector3 right { get { return rotation * Vector3.right; } }

        public OrientationState(Quaternion rotation, Vector3 angularVelocity, Vector3 angularAcceleration)
        {
            this.rotation = rotation;
            this.angularVelocity = angularVelocity;
            this.angularAcceleration = angularAcceleration;
        }

        private static Vector3 getStartVelocity(GameObject target)
        {
            var initialMotion = target.GetComponent<InitialMotion>();
            var initialVelocity = target.GetComponent<InitialVelocity>();

            if (initialMotion != null)
            {
                return initialMotion.GetInitAngularVelocity();
            }
            else if (initialVelocity != null)
            {
                return initialVelocity.GetValue<Vector3>("initAngularVelocityAxis").normalized * initialVelocity.GetValue<float>("initAngularVelocityMagnitude");
            }
            else
            {
                return Vector3.zero;
            }
        }

        public static OrientationState fromCurrentState(HeavenlyBody target, bool useInitialVelocity = false)
        {
            return fromCurrentState(Position.getBody(target), useInitialVelocity);
        }

        public static OrientationState fromCurrentState(OWRigidbody target, bool useInitialVelocity = false)
        {
            if (target == null || target.GetRigidbody() == null)
            {
                return null;
            }

            var rotation = target.GetRotation();
            var angularVelocity = target.GetAngularVelocity();
            var angularAcceleration = target.GetAngularAcceleration();

            if (useInitialVelocity)
            {
                angularVelocity = getStartVelocity(target.gameObject);
                angularAcceleration = Vector3.zero;
            }

            return new OrientationState(rotation, angularVelocity, angularAcceleration);
        }

        private static OrientationState fromCurrentState(KinematicRigidbody target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var rotation = target.IntegrateRotation();
            var angularVelocity = target.angularVelocity;
            var angularAcceleration = Vector3.zero;

            if (useInitialVelocity)
            {
                angularVelocity = getStartVelocity(target.gameObject);
                angularAcceleration = Vector3.zero;
            }

            return new OrientationState(rotation, angularVelocity, angularAcceleration);
        }

        private static OrientationState fromCurrentState(Rigidbody target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var rotation = target.rotation;
            var angularVelocity = target.angularVelocity;
            var angularAcceleration = Vector3.zero;

            if (useInitialVelocity)
            {
                angularVelocity = getStartVelocity(target.gameObject);
                angularAcceleration = Vector3.zero;
            }

            return new OrientationState(rotation, angularVelocity, angularAcceleration);
        }

        public static OrientationState fromCurrentState(Transform target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var rotation = target.rotation;
            var angularVelocity = Vector3.zero;
            var angularAcceleration = Vector3.zero;

            if (useInitialVelocity)
            {
                angularVelocity = getStartVelocity(target.gameObject);
                angularAcceleration = Vector3.zero;
            }

            return new OrientationState(rotation, angularVelocity, angularAcceleration);
        }

        public static OrientationState fromCurrentState(GameObject target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var owRigidBody = target.GetComponent<OWRigidbody>();
            var transform = target.transform;

            if (owRigidBody != null)
            {
                return fromCurrentState(owRigidBody, useInitialVelocity);
            }
            else
            {
                return fromCurrentState(transform, useInitialVelocity);
            }
        }

        public override string ToString()
        {
            var orientation = this.rotation == null ? "" : DisplayConsole.logQuaternion(this.rotation);
            var angularVelocity = this.angularVelocity == null ? "" : DisplayConsole.logVector(this.angularVelocity);
            var angularAcceleration = this.angularAcceleration == null ? "" : DisplayConsole.logVector(this.angularAcceleration);
            return $"({orientation}, {angularVelocity}, {angularAcceleration})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is OrientationState)
            {
                return Equals((OrientationState)(other as OrientationState));
            }
            return false;
        }

        public bool Equals(OrientationState other)
        {
            if (other != null)
            {
                return ((rotation == null && other.rotation == null) || rotation.Equals(other.rotation))
                    && ((angularVelocity == null && other.angularVelocity == null) || angularVelocity.Equals(other.angularVelocity))
                    && ((angularAcceleration == null && other.angularAcceleration == null) || angularAcceleration.Equals(other.angularAcceleration));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (rotation.GetHashCode() * 4)
                + (angularVelocity.GetHashCode() * 16)
                + (angularAcceleration.GetHashCode() * 64);
        }
    }

    public class KeplerState : IEquatable<KeplerState>
    {
        public ScaleState scale { get; }
        public KeplerCoordinates coordinates { get; }
        public OrientationState orientation { get; }

        public Vector3 lossyScale { get { return scale?.lossyScale ?? ScaleState.identity.lossyScale; } }
        public Vector3 localScale { get { return scale?.localScale ?? ScaleState.identity.localScale; } }

        public Quaternion rotation { get { return orientation?.rotation ?? OrientationState.identity.rotation; } }
        public Vector3 angularVelocity { get { return orientation?.angularVelocity ?? OrientationState.identity.angularVelocity; } }
        public Vector3 angularAcceleration { get { return orientation?.angularAcceleration ?? OrientationState.identity.angularAcceleration; } }

        public KeplerState(ScaleState scale, KeplerCoordinates coordinates, OrientationState orientation)
        {
            this.scale = scale ?? ScaleState.identity;
            this.coordinates = coordinates;
            this.orientation = orientation ?? OrientationState.identity;
        }

        public override string ToString()
        {
            return $"({scale}, {coordinates}, {orientation})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is KeplerState)
            {
                return Equals((KeplerState)(other as KeplerState));
            }
            return false;
        }

        public bool Equals(KeplerState other)
        {
            if (other != null)
            {
                return ((scale == null && other.scale == null) || scale.Equals(other.scale))
                    && ((coordinates == null && other.coordinates == null) || coordinates.Equals(other.coordinates))
                    && ((orientation == null && other.orientation == null) || orientation.Equals(other.orientation));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (scale.GetHashCode())
                + (coordinates.GetHashCode() * 4)
                + (orientation.GetHashCode() * 16);
        }
    }

    public class MovementState : IEquatable<MovementState>
    {
        public ScaleState scale { get; }
        public PositionState coordinates { get; }
        public OrientationState orientation { get; }

        public Vector3 lossyScale { get { return scale?.lossyScale ?? ScaleState.identity.lossyScale; } }
        public Vector3 localScale { get { return scale?.localScale ?? ScaleState.identity.localScale; } }

        public Vector3 position { get { return coordinates?.position ?? PositionState.identity.position; } }
        public Vector3 velocity { get { return coordinates?.velocity ?? PositionState.identity.velocity; } }
        public Vector3 acceleration { get { return coordinates?.acceleration ?? PositionState.identity.acceleration; } }
        public Vector3 jerk { get { return coordinates?.jerk ?? PositionState.identity.jerk; } }

        public Quaternion rotation { get { return orientation?.rotation ?? OrientationState.identity.rotation; } }
        public Vector3 angularVelocity { get { return orientation?.angularVelocity ?? OrientationState.identity.angularVelocity; } }
        public Vector3 angularAcceleration { get { return orientation?.angularAcceleration ?? OrientationState.identity.angularAcceleration; } }

        public MovementState(ScaleState scale, PositionState coordinates, OrientationState orientation)
        {
            this.scale = scale ?? ScaleState.identity;
            this.coordinates = coordinates ?? PositionState.identity;
            this.orientation = orientation ?? OrientationState.identity;
        }

        public override string ToString()
        {
            return $"({scale}, {coordinates}, {orientation})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is MovementState)
            {
                return Equals((MovementState)(other as MovementState));
            }
            return false;
        }

        public bool Equals(MovementState other)
        {
            if (other != null)
            {
                return ((scale == null && other.scale == null) || scale.Equals(other.scale))
                    && ((coordinates == null && other.coordinates == null) || coordinates.Equals(other.coordinates))
                    && ((orientation == null && other.orientation == null) || orientation.Equals(other.orientation));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (scale.GetHashCode())
                + (coordinates.GetHashCode() * 4)
                + (orientation.GetHashCode() * 16);
        }
    }

    public class AbsoluteState : MovementState, IEquatable<AbsoluteState>
    {
        public AbsoluteState(ScaleState scale, PositionState coordinates, OrientationState orientation) : base(scale, coordinates, orientation)
        {
        }

        public static AbsoluteState fromCurrentState(HeavenlyBody target, bool useInitialVelocity = false)
        {
            return fromCurrentState(Position.getBody(target), useInitialVelocity);
        }

        public static AbsoluteState fromCurrentState(OWRigidbody target, bool useInitialVelocity = false)
        {
            if (target == null || target.GetRigidbody() == null)
            {
                return null;
            }
            return new AbsoluteState(ScaleState.fromCurrentState(target), PositionState.fromCurrentState(target, useInitialVelocity), OrientationState.fromCurrentState(target, useInitialVelocity));
        }

        public static AbsoluteState fromCurrentState(Transform target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }
            return new AbsoluteState(ScaleState.fromCurrentState(target), PositionState.fromCurrentState(target, useInitialVelocity), OrientationState.fromCurrentState(target, useInitialVelocity));
        }

        public static AbsoluteState fromCurrentState(GameObject target, bool useInitialVelocity = false)
        {
            if (target == null)
            {
                return null;
            }

            var rigidBody = target.GetComponent<OWRigidbody>();
            var transform = target.transform;

            if (rigidBody != null)
            {
                return fromCurrentState(target, useInitialVelocity);
            }
            else
            {
                return fromCurrentState(transform, useInitialVelocity);
            }
        }

        public override string ToString()
        {
            return $"({scale}, {coordinates}, {orientation})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is AbsoluteState)
            {
                return Equals((AbsoluteState)(other as AbsoluteState));
            }
            else if (other != null && other is MovementState)
            {
                return Equals((MovementState)(other as MovementState));
            }
            return false;
        }

        public bool Equals(AbsoluteState other)
        {
            if (other != null)
            {
                return ((scale == null && other.scale == null) || scale.Equals(other.scale))
                    && ((coordinates == null && other.coordinates == null) || coordinates.Equals(other.coordinates))
                    && ((orientation == null && other.orientation == null) || orientation.Equals(other.orientation));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (scale.GetHashCode())
                + (coordinates.GetHashCode() * 4)
                + (orientation.GetHashCode() * 16);
        }

        public Vector3 GetPointVelocity(Vector3 worldPoint) => this.GetPointTangentialVelocity(worldPoint) + velocity;

        public Vector3 GetPointTangentialVelocity(Vector3 worldPoint) => Vector3.Cross(angularVelocity, worldPoint - position);

        public Vector3 GetPointAcceleration(Vector3 worldPoint)
        {
            var rhs = worldPoint - position;
            return acceleration + Vector3.Cross(angularVelocity, Vector3.Cross(angularVelocity, rhs)) + Vector3.Cross(angularAcceleration, rhs);
        }

        public Vector3 GetPointTangentialAcceleration(Vector3 worldPoint)
        {
            var rhs = worldPoint - position;
            return Vector3.Cross(angularAcceleration, rhs);
        }

        public Vector3 GetPointCentripetalAcceleration(Vector3 worldPoint)
        {
            return Vector3.Cross(angularVelocity, Vector3.Cross(angularVelocity, worldPoint - position));
        }

        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return rotation * localDirection;
        }

        public Vector3 InverseTransformDirection(Vector3 worldDirection)
        {
            return Quaternion.Inverse(rotation) * worldDirection;
        }

        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return position + rotation * Vector3.Scale(lossyScale, localPoint);
        }

        public Vector3 InverseTransformPoint(Vector3 worldPoint)
        {
            return Vector3.Scale(new Vector3(1 / lossyScale.x, 1 / lossyScale.y, 1 / lossyScale.z),
               Quaternion.Inverse(rotation) * (worldPoint - position));
        }

        public Quaternion PointRotation(Vector3 worldPoint)
        {
            var groundNormal = worldPoint - position;

            if (groundNormal.z < 0)
            {
                var forwardDirection = -Vector3.Cross(groundNormal, orientation.right);
                return Quaternion.LookRotation(forwardDirection, groundNormal);
            }
            else
            {
                var forwardDirection = -Vector3.Cross(groundNormal, orientation.left);
                return Quaternion.LookRotation(forwardDirection, groundNormal);
            }
        }

        public void apply(HeavenlyBody parent, AbsoluteState parentState, OWRigidbody target)
        {
            applyMovement(target);
            applyRotation(target);
            applyCachedState(parent, parentState, target);
        }

        private void applyMovement(OWRigidbody target)
        {
            var offset = PositionState.offset;
            var p = position + offset.position;
            var v = velocity + offset.velocity; /* Possibly not required because of CenterOfTheUniverseOffsetApplier */
            var a = acceleration + offset.acceleration;
            var j = jerk + offset.jerk;

            target.SetPosition(new Vector3(p.x, p.y, p.z));

            if (target.RunningKinematicSimulation())
            {
                target.GetValue<KinematicRigidbody>("_kinematicRigidbody").velocity = new Vector3(v.x, v.y, v.z);
            }
            else
            {
                target.GetRigidbody().velocity = new Vector3(v.x, v.y, v.z);
            }

            target.SetValue("_lastPosition", new Vector3(p.x, p.y, p.z));
            target.SetValue("_currentVelocity", new Vector3(v.x, v.y, v.z));
            target.SetValue("_lastVelocity", new Vector3(v.x, v.y, v.z));
            target.SetValue("_currentAccel", new Vector3(a.x, a.y, a.z));
            target.SetValue("_lastAccel", new Vector3(a.x, a.y, a.z));
        }

        private void applyRotation(OWRigidbody target)
        {
            var r = rotation;
            var v = angularVelocity;
            var a = angularAcceleration;

            target.SetRotation(new Quaternion(r.x, r.y, r.z, r.w));
            target.SetAngularVelocity(new Vector3(v.x, v.y, v.z));

            target.SetValue("_currentAngularVelocity", new Vector3(v.x, v.y, v.z));
            target.SetValue("_lastAngularVelocity", new Vector3(v.x, v.y, v.z));
        }

        private void applyCachedState(HeavenlyBody parent, AbsoluteState parentState, OWRigidbody target)
        {
            if (target.IsSuspended())
            {
                var parentBody = Position.getBody(parent);
                target.SetValue("_suspensionBody", parentBody);
                target.SetValue("_cachedRelativeVelocity", (parentBody == null || parentState == null) ? Vector3.zero : parentState.InverseTransformDirection(velocity - parentState.GetPointVelocity(position)));
                target.SetValue("_cachedAngularVelocity", angularVelocity);
            }
        }
    }

    public class RelativeState : IEquatable<RelativeState>
    {
        public HeavenlyBody parent { get; }
        public MovementState relative { get; }
        public MovementState surface { get; }
        public KeplerState orbit { get; }


        private RelativeState(HeavenlyBody parent, MovementState relative, MovementState surface, KeplerState orbit)
        {
            this.parent = parent;
            this.relative = relative;
            this.surface = surface;
            this.orbit = orbit;
        }

        public override string ToString()
        {
            var relativeString = this.relative == null ? "" : $", Relative: {this.relative.ToString()}";
            var surfaceString = this.surface == null ? "" : $", Surface: {this.surface.ToString()}";
            var orbitString = this.orbit == null ? "" : $", Orbit: {this.orbit.ToString()}";
            return $"({parent}{relativeString}{surfaceString}{orbitString})";
        }

        public override bool Equals(System.Object other)
        {
            if (other != null && other is RelativeState)
            {
                return Equals((RelativeState)(other as RelativeState));
            }
            return false;
        }

        public bool Equals(RelativeState other)
        {
            if (other != null)
            {
                return ((parent == null && other.parent == null) || parent.Equals(other.parent))
                    && ((relative == null && other.relative == null) || relative.Equals(other.relative))
                    && ((surface == null && other.surface == null) || surface.Equals(other.surface))
                    && ((orbit == null && other.orbit == null) || orbit.Equals(other.orbit));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (parent.GetHashCode())
                + (relative.GetHashCode() >> 2)
                + (surface.GetHashCode() >> 4)
                + (orbit.GetHashCode() >> 6);
        }

        public AbsoluteState apply(OWRigidbody body)
        {
            var parentState = AbsoluteState.fromCurrentState(parent);
            var parentGravity = parent.pseudoHeavenlyBody ? Planet.getParentGravity(Position.find(body)) : Planet.getGravity(parent);

            return apply(body, parentState, parentGravity);
        }

        public AbsoluteState apply(OWRigidbody body, AbsoluteState parentState, Gravity gravity)
        {
            var state = getAbsoluteState(parentState, gravity, body);

            if (state != null)
            {
                state.apply(parent, parentState, body);
            }
            return state;
        }

        public AbsoluteState getAbsoluteState(OWRigidbody body)
        {
            var parentState = AbsoluteState.fromCurrentState(parent);
            var parentGravity = parent.pseudoHeavenlyBody ? Planet.getParentGravity(Position.find(body)) : Planet.getGravity(parent);

            return getAbsoluteState(parentState, parentGravity, body);
        }

        public AbsoluteState getAbsoluteState(AbsoluteState parentState, Gravity gravity, OWRigidbody body)
        {
            var scale = getAbsoluteScale(parentState, gravity);
            if (scale == null)
            {
                return null;
            }

            var movement = getAbsoluteMovement(parentState, gravity);
            if (movement == null)
            {
                return null;
            }

            var orientation = getAbsoluteOrientation(parentState, gravity, movement.position, body);
            if (orientation == null)
            {
                return null;
            }

            return new AbsoluteState(scale, movement, orientation);
        }


        private ScaleState getAbsoluteScale(AbsoluteState parentState, Gravity gravity)
        {
            if (orbit != null && orbit.coordinates != null && orbit.coordinates.isOrbit() && gravity != null)
            {
                return orbit.scale;
            }
            else if (parentState != null
                && surface != null)
            {
                return surface.scale;
            }
            else if (relative != null)
            {
                return relative.scale;
            }

            return null;
        }

        private PositionState getAbsoluteMovement(AbsoluteState parentState, Gravity gravity)
        {
            if (orbit != null && orbit.coordinates != null && orbit.coordinates.isOrbit() && gravity != null)
            {
                return getAbsoluteFromKeplerState(parentState, gravity);
            }
            else if (parentState != null
                && surface != null)
            {
                return getAbsoluteFromSurfaceState(parentState);
            }
            else if (relative != null)
            {
                return getAbsoluteFromRelativeState(parentState);
            }

            return null;
        }

        private PositionState getAbsoluteFromKeplerState(AbsoluteState parentState, Gravity gravity)
        {
            var cartesian = OrbitHelper.toCartesian(gravity, Time.timeSinceLevelLoad, orbit.coordinates);
            var position = cartesian.Item1;
            var velocity = cartesian.Item2;
            var acceleration = Vector3.zero;
            var jerk = Vector3.zero;

            if (parentState != null && surface != null)
            {
                position = surface.position.normalized * position.magnitude;
                position = parentState.TransformPoint(position);
                velocity += parentState.velocity;
                // TODO: Fix velocity to point in correct direction
            }
            else if (parentState != null)
            {
                position += parentState.position;
                velocity += parentState.velocity;
            }

            return new PositionState(position, velocity, acceleration, jerk);
        }

        private PositionState getAbsoluteFromSurfaceState(AbsoluteState parentState)
        {
            var position = this.surface.position;
            var velocity = this.surface.velocity;
            var acceleration = this.surface.acceleration;
            var jerk = this.surface.jerk;

            position = parentState.TransformPoint(position);
            velocity += parentState.GetPointVelocity(position);
            acceleration += parentState.GetPointAcceleration(position);

            return new PositionState(position, velocity, acceleration, jerk);
        }

        private PositionState getAbsoluteFromRelativeState(AbsoluteState parentState)
        {
            var position = this.relative.position;
            var velocity = this.relative.velocity;
            var acceleration = this.relative.acceleration;
            var jerk = this.relative.jerk;

            if (parentState != null)
            {
                position += parentState.position;
                velocity += parentState.velocity;
                acceleration += parentState.acceleration;
            }

            return new PositionState(position, velocity, acceleration, jerk);
        }

        private OrientationState getAbsoluteOrientation(AbsoluteState parentState, Gravity gravity, Vector3 worldPosition, OWRigidbody target)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3 angularVelocity = Vector3.zero;
            Vector3 angularAcceleration = Vector3.zero;

            var alignment = target.GetComponent<AlignWithTargetBody>();
            if (parentState != null && orbit != null && orbit.coordinates != null && orbit.coordinates.isOrbit() && gravity != null)
            {
                var pointRotation = parentState.PointRotation(worldPosition);
                rotation = pointRotation * orbit.rotation;
                angularVelocity = pointRotation * orbit.angularVelocity;
                angularAcceleration = Vector3.zero;

                var targetDirection = parentState.position - worldPosition;
            }
            else if (parentState != null && surface != null)
            {
                var pointRotation = parentState.PointRotation(worldPosition);
                rotation = pointRotation * surface.rotation;
                angularVelocity = pointRotation * surface.angularVelocity;
                angularAcceleration = Vector3.zero;
            }
            else if (alignment != null
                && parentState != null)
            {
                var alignmentAxis = alignment._localAlignmentAxis;
                var targetDirection = parentState.position - worldPosition;
                var pointRotation = Quaternion.FromToRotation(alignmentAxis, targetDirection);

                rotation = pointRotation * relative.rotation;
                rotation = rotation * Quaternion.AngleAxis(180, alignment._localAlignmentAxis);

                angularVelocity = Vector3.zero;
                angularAcceleration = Vector3.zero;
            }
            else if (relative != null)
            {
                rotation = relative.rotation;
                angularVelocity = relative.angularVelocity;
                angularAcceleration = Vector3.zero;
            }

            return new OrientationState(rotation, angularVelocity, angularAcceleration);
        }

        public static RelativeState fromGlobal(HeavenlyBody parent, OWRigidbody target, bool useInitialVelocity = false)
        {
            var parentGravity = parent.pseudoHeavenlyBody ? Planet.getParentGravity(Position.find(target)) : Planet.getGravity(parent);
            var parentSize = parent.pseudoHeavenlyBody ? new Position.Size(0, 45000) : Planet.getSize(parent);
            return fromGlobal(parent, AbsoluteState.fromCurrentState(Position.getBody(parent), useInitialVelocity), parentGravity, parentSize, ScaleState.fromCurrentState(target), AbsoluteState.fromCurrentState(target, useInitialVelocity));
        }

        public static RelativeState fromGlobal(HeavenlyBody parent, AbsoluteState parentState, Gravity parentGravity, Position.Size parentSize, ScaleState targetScale, AbsoluteState target)
        {
            MovementState surfaceMovement = null;
            KeplerState orbit = null;

            parent = parentState == null ? HeavenlyBody.None : parent;
            var relativeMovement = getRelativeMovement(parentState, parentGravity, targetScale, target);
            if (relativeMovement == null)
            {
                return null;
            }
            else if (parentState != null && parentSize != null)
            {
                var distance = relativeMovement.position.sqrMagnitude;
                if (distance < (parentSize.influence * parentSize.influence))
                {
                    surfaceMovement = getSurfaceMovement(parentState, parentGravity, targetScale, target);
                }

                if ((parentSize.size * parentSize.size) < distance && distance < (parentSize.influence * parentSize.influence))
                {
                    var kepler = getKepler(parentState, parentGravity, targetScale, target);
                    if (kepler != null && kepler.coordinates != null && kepler.coordinates.isOrbit())
                    {
                        if (parentSize.size < kepler.coordinates.perigee && kepler.coordinates.apogee < parentSize.influence)
                        {
                            orbit = kepler;
                        }
                    }
                }
            }

            return new RelativeState(parent, relativeMovement, surfaceMovement, orbit);
        }

        public static MovementState getRelativeMovement(HeavenlyBody parent, OWRigidbody target)
        {
            var parentState = AbsoluteState.fromCurrentState(Position.getBody(parent));
            var targetState = AbsoluteState.fromCurrentState(target);
            var parentGravity = parent.pseudoHeavenlyBody ? Planet.getParentGravity(Position.find(target)) : Planet.getGravity(parent);
            var alignment = target.GetComponent<AlignWithTargetBody>();
            if (alignment != null)
            {
                var newOrientation = getAlignWithTargetBodyOrientation(parentState, targetState, target);
                targetState = new AbsoluteState(targetState.scale, targetState.coordinates, newOrientation);
            }
            return getRelativeMovement(parentState, parentGravity, ScaleState.fromCurrentState(target), targetState);
        }

        public static MovementState getRelativeMovement(AbsoluteState parentState, Gravity parentGravity, ScaleState targetScale, AbsoluteState target)
        {
            if (target == null)
            {
                return null;
            }

            var relativePosition = target.position - (parentState == null ? Vector3.zero : parentState.position);
            var relativeVelocity = target.velocity - (parentState == null ? Vector3.zero : parentState.velocity);
            var relativeAcceleration = target.acceleration - (parentState == null ? Vector3.zero : parentState.acceleration);
            var relativeJerk = target.jerk - (parentState == null ? Vector3.zero : parentState.jerk);
            var relativeOrientation = target.rotation;
            var relativeAngularVelocity = target.angularVelocity;
            var relativeAngularAcceleration = target.angularAcceleration;

            return new MovementState(targetScale, new PositionState(relativePosition, relativeVelocity, relativeAcceleration, relativeJerk), new OrientationState(relativeOrientation, relativeAngularVelocity, relativeAngularAcceleration));
        }

        public static MovementState getSurfaceMovement(HeavenlyBody parent, OWRigidbody target)
        {
            var parentGravity = parent.pseudoHeavenlyBody ? Planet.getParentGravity(Position.find(target)) : Planet.getGravity(parent);
            return getSurfaceMovement(AbsoluteState.fromCurrentState(Position.getBody(parent)), parentGravity, ScaleState.fromCurrentState(target), AbsoluteState.fromCurrentState(target));
        }

        public static MovementState getSurfaceMovement(AbsoluteState parentState, Gravity parentGravity, ScaleState targetScale, AbsoluteState target)
        {
            if (parentState == null || target == null)
            {
                return null;
            }

            var surfacePosition = parentState.InverseTransformPoint(target.position);
            var surfaceVelocity = target.velocity - parentState.GetPointVelocity(target.position);
            var surfaceAcceleration = target.acceleration - parentState.GetPointAcceleration(target.position);
            var surfaceJerk = target.jerk - (parentState == null ? Vector3.zero : parentState.jerk);

            var pointRotation = Quaternion.Inverse(parentState.PointRotation(target.position));
            var surfaceRotation = pointRotation * target.rotation;
            var surfaceAngularVelocity = pointRotation * target.angularVelocity;
            var surfaceAngularAcceleration = pointRotation * target.angularAcceleration;

            return new MovementState(targetScale, new PositionState(surfacePosition, surfaceVelocity, surfaceAcceleration, surfaceJerk), new OrientationState(surfaceRotation, surfaceAngularVelocity, surfaceAngularAcceleration));
        }

        public static KeplerState getKepler(HeavenlyBody parent, OWRigidbody target)
        {
            var parentGravity = parent.pseudoHeavenlyBody ? Planet.getParentGravity(Position.find(target)) : Planet.getGravity(parent);
            return getKepler(AbsoluteState.fromCurrentState(Position.getBody(parent)), parentGravity, ScaleState.fromCurrentState(target), AbsoluteState.fromCurrentState(target));
        }

        public static KeplerState getKepler(AbsoluteState parentState, Gravity parentGravity, ScaleState targetScale, AbsoluteState target)
        {
            if (parentState == null || parentGravity == null || target == null)
            {
                return null;
            }
            var absolutePosition = target.position;
            var absoluteVelocity = target.velocity;

            var pointRotation = Quaternion.Inverse(parentState.PointRotation(target.position));
            var keplerOrientation = pointRotation * target.rotation;
            var keplerAngularVelocity = pointRotation * target.angularVelocity;
            var keplerAngularAcceleration = pointRotation * target.angularAcceleration;

            return new KeplerState(targetScale, Position.getKepler(parentState, parentGravity, absolutePosition, absoluteVelocity), new OrientationState(keplerOrientation, keplerAngularVelocity, keplerAngularAcceleration));
        }

        private static OrientationState getAlignWithTargetBodyOrientation(AbsoluteState parentState, AbsoluteState targetState, OWRigidbody target)
        {
            var alignment = target.GetComponent<AlignWithTargetBody>();
            if (alignment != null
                && parentState != null)
            {
                var alignmentAxis = alignment.GetValue<Vector3>("_localAlignmentAxis");
                var targetDirection = parentState.position - targetState.position;

                var pointRotation = Quaternion.FromToRotation(alignmentAxis, targetDirection);

                // All AlignWithTargetBody's are wrong
                pointRotation = pointRotation * Quaternion.AngleAxis(90, alignment._localAlignmentAxis);

                var targetOrientation = Quaternion.Inverse(pointRotation) * targetState.rotation;
                var targetVelocity = Vector3.zero;
                var targetAcceleration = Vector3.zero;

                return new OrientationState(targetOrientation, targetVelocity, targetAcceleration);
            }

            return null;
        }

        public static RelativeState fromRelative(HeavenlyBody parent, MovementState relative)
        {
            if (relative == null)
            {
                return null;
            }
            return new RelativeState(parent, relative, null, null);
        }

        public static RelativeState fromSurface(HeavenlyBody parent, MovementState surface)
        {
            if (surface == null)
            {
                return null;
            }
            return new RelativeState(parent, null, surface, null);
        }

        public static RelativeState fromKepler(HeavenlyBody parent, ScaleState targetScale, KeplerCoordinates kepler, OrientationState orientation)
        {
            if (kepler != null && kepler.isOrbit())
            {
                return new RelativeState(parent, null, null, new KeplerState(targetScale, kepler, orientation));
            }
            return null;
        }

        public static RelativeState fromClosetInfluence(OWRigidbody target, params HeavenlyBody[] exclude)
        {
            var targetState = PositionState.fromCurrentState(target);
            if (targetState == null)
            {
                return null;
            }

            var parent = Position.getClosetInfluence(targetState.position, Position.getBodies(), exclude);

            if (parent.Count < 1)
            {
                return null;
            }
            return fromGlobal(parent[0].Item1, target);
        }
    }
}

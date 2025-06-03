using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit2
{
    public static class QuaternionExtend
    {
		public static readonly Quaternion Default = new Quaternion(0f, 0f, 0f, 0f);
		public static bool IsInvalid(this Quaternion self)
		{
			return
				self.x == 0f && !float.IsNaN(self.x) &&
				self.y == 0f && !float.IsNaN(self.y) &&
				self.z == 0f && !float.IsNaN(self.z) &&
				self.w == 0f && !float.IsNaN(self.w);
        }

		public static bool IsNaN(this Quaternion self)
		{
			return
				!float.IsNaN(self.x) &&
				!float.IsNaN(self.y) &&
				!float.IsNaN(self.z) &&
				!float.IsNaN(self.w);
		}
		
		/// <summary>Get the different between two Quaternion</summary>
		/// <param name="qtA"></param>
		/// <param name="qtB"></param>
		/// <returns>Different in Quaternion format</returns>
		/// <see cref="http://answers.unity3d.com/questions/35541/problem-finding-relative-rotation-from-one-quatern.html"/>
		public static Quaternion Different(this Quaternion from, Quaternion to)
        {
            return from.Inverse() * to;
        }

        /// <summary>The relative rotation between current and next</summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Quaternion FromToRotation(this Quaternion from, Quaternion to)
        {
            return from.Inverse() * to;
        }

        public static Quaternion Add(this Quaternion start, Quaternion diff)
        {
			return diff * start;
        }

        /// <summary>Shortcut for Inverse rotation</summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static Quaternion Inverse(this Quaternion self)
        {
            return Quaternion.Inverse(self);
        }

		/// <summary>Get conjugate quaternion based on current</summary>
		/// <param name="self"></param>
		/// <returns></returns>
		/// <see cref="http://stackoverflow.com/questions/22157435/difference-between-the-two-quaternions"/>
		public static Quaternion Conjugate(this Quaternion self)
        {
            return new Quaternion(-self.x, -self.y, -self.z, self.w);
        }

		/// <summary>Get the local right vector from quaternion.</summary>
		/// <param name="rotation"></param>
		/// <returns>Vector direction</returns>
		public static Vector3 GetRight(this Quaternion rotation)
        {
            return rotation * Vector3.right;
        }

		/// <summary>Get the local left vector from quaternion.</summary>
		/// <param name="rotation"></param>
		/// <returns>Vector direction</returns>
		public static Vector3 GetLeft(this Quaternion rotation)
		{
			return rotation * Vector3.left;
		}

		/// <summary>Get the local up vector from quaternion.</summary>
		/// <param name="rotation"></param>
		/// <returns>Vector direction</returns>
		public static Vector3 GetUp(this Quaternion rotation)
        {
            return rotation * Vector3.up;
        }

		/// <summary>Get the local down vector from quaternion.</summary>
		/// <param name="rotation"></param>
		/// <returns>Vector direction</returns>
		public static Vector3 GetDown(this Quaternion rotation)
		{
			return rotation * Vector3.down;
		}

        /// <summary>Get the local forward vector from quaternion.</summary>
        /// <param name="rotation"></param>
        /// <returns>Vector direction</returns>
        public static Vector3 GetForward(this Quaternion rotation)
        {
            return rotation * Vector3.forward;
        }

		/// <summary>Get the local backward vector from quaternion.</summary>
		/// <param name="rotation"></param>
		/// <returns>Vector direction</returns>
		public static Vector3 GetBackward(this Quaternion rotation)
		{
			return rotation * Vector3.back;
		}

		/// <see cref="http://sunday-lab.blogspot.hk/2008/04/get-pitch-yaw-roll-from-quaternion.html"/>
		/// <summary>Calculate pitch angle based on Vector3.forward & Vector3.up</summary>
		/// <returns>Pitch angle based on world forward & upward.</returns>
		public static float GetPitch(this Quaternion o)
        {
            return Mathf.Atan((2f * (o.y * o.z + o.w * o.x)) / (o.w * o.w - o.x * o.x - o.y * o.y + o.z * o.z));
        }

		/// <summary>Calculate pitch angle based on Vector3.forward & Vector3.up</summary>
		/// <returns>Yaw angle based on world forward & upward.</returns>
		public static float GetYaw(this Quaternion o)
        {
            return Mathf.Asin(-2f * (o.x * o.z - o.w * o.y));
        }

		/// <summary>Calculate pitch angle based on Vector3.forward & Vector3.up</summary>
		/// <returns>Roll angle based on world forward & upward.</returns>
		public static float GetRoll(this Quaternion o)
        {
            return Mathf.Atan((2f * (o.x * o.y + o.w * o.z)) / (o.w * o.w + o.x * o.x - o.y * o.y - o.z * o.z));
        }

        /// <summary>To find a signed angle between two quaternion, based on giving axis</summary>
        /// <param name="qtA">Quaternion</param>
        /// <param name="qtB">Quaternion</param>
        /// <param name="localDirection">The direction of quaternion, e.g. Vector3.forward</param>
        /// <param name="normal">The normal axis, e.g. Vector3.right</param>
        /// <returns></returns>
        public static float AngleBetweenDirectionSigned(this Quaternion qtA, Quaternion qtB, Vector3 localDirection, Vector3 normal)
        {
            return (qtA * localDirection).AngleBetweenDirectionSigned(qtB * localDirection, normal);
        }

		/// <summary>Bias Warning
		/// Attempt to find local axis angle between giving Quaterion
		/// Since quaterion angle didn't support "Sign", it aren't really make sense in 3D space.
		/// because it's moving the normal can't be trusted.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns>return signed angle, bias on <paramref name="from"/> rotation</returns>
		public static float AngleProjectOnPlaneSigned(this Quaternion from, Quaternion to, Vector3 localDirection, Vector3 normal)
		{
			return (from * localDirection).AngleProjectOnPlaneSigned(to * localDirection, from * normal);
        }

		/// <summary>Get rotation angle and project on plane.</summary>
		/// <param name="qt"></param>
		/// <param name="planeNormal"></param>
		/// <returns></returns>
		/// <remarks>This are different with <see cref="Limit1DOF(Quaternion, Vector3)"/>
		/// since its projection, it may result in flip forword in action.
		/// </remarks>
		public static Quaternion ProjectOnPlane(this Quaternion qt, Vector3 planeNormal)
		{
			Vector3 forward = qt * Vector3.forward;
			forward = Vector3.ProjectOnPlane(forward, planeNormal);
			if (forward == Vector3.zero)
				return Quaternion.identity;
			return Quaternion.LookRotation(forward, planeNormal);
		}

		/// <summary>Limits rotation to a single degree of freedom (along axis)</summary>
		/// <param name="rotation"></param>
		/// <param name="axis">The dimension we wanted to Keep.</param>
		/// <returns></returns>
		public static Quaternion Limit1DOF(this Quaternion rotation, Vector3 axis)
		{
			/*
			Vector3 destination = rotation * axis;
			Vector3 from = axis;
			// Since Vector3 can't contain the rotation along giving axis;
			// the movement will lose those data.
			Quaternion movmentMissingRotateAlongAxis = Quaternion.FromToRotation(from, destination);
			Quaternion movementToCancel = Quaternion.Inverse(movmentMissingRotateAlongAxis);
			Quaternion angularMomentum = rotation; // Regard current rotation as angular momentum.
			// As result, we get the Angular momentum that only remain the rotation along axis.
			return movementToCancel * angularMomentum;
			*/
			return Quaternion.FromToRotation(rotation * axis, axis) * rotation;
		}

		/// <summary>The extra FromToRotation with projection,
		/// in order to solve when the reference direction (forward) align with (axis)</summary>
		/// <param name="self"></param>
		/// <param name="other"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static Quaternion ProjectFromToRotation(this Quaternion self, Quaternion other, Vector3 axis)
		{
			return Quaternion.FromToRotation(self * axis, other * axis);
		}

		/// <summary>
		/// <see cref="https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Source_Code_2"/>
		/// <see cref="https://answers.unity.com/questions/416169/finding-pitchrollyaw-from-quaternions.html"/>
		/// <seealso cref="https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles"/>
		/// </summary>
		/// <param name="rotation"></param>
		/// <param name="roll"></param>
		/// <param name="yaw"></param>
		/// <param name="pitch"></param>
		public static void ResolveYawPitchRoll(this Quaternion rotation, bool degree, out float pitch, out float yaw, out float roll)
		{
			float x = rotation.x;
			float y = rotation.y;
			float z = rotation.z;
			float w = rotation.w;
			// roll (x-axis rotation)
			float sinr_cosp = 2f * (w * x + y * z);
			float cosr_cosp = 1f - 2f * (x * x + y * y);
			pitch = Mathf.Atan2(sinr_cosp, cosr_cosp);

			// pitch (y-axis rotation)
			float sinp = 2f * (w * y - z * x);
			yaw = Mathf.Abs(sinp) >= 1 ? Mathf.Sign(sinp) * Mathf.PI / 2f : Mathf.Asin(sinp);

			// Yaw (z-axis rotation)
			float siny_cosp = 2f * (w * z + x * y);
			float cosy_cosp = 1f - 2f * (y * y + z * z);
			roll = Mathf.Atan2(siny_cosp, cosy_cosp);
			if (degree)
			{
				pitch *= Mathf.Rad2Deg;
				yaw *= Mathf.Rad2Deg;
				roll *= Mathf.Rad2Deg;
			}
		}

		/// <summary>A wrapper to create rotation based on yaw, pitch, rool, from <see cref="System.Numerics.Quaternion"/>
		/// <see cref="https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Source_Code_2"/>
		/// <see cref="https://github.com/NickCuso/Tutorials/blob/master/Quaternions.md"/></summary>
		/// <param name="roll"></param>
		/// <param name="yaw"></param>
		/// <param name="pitch"></param>
		/// <returns></returns>
		public static Quaternion CreateFromYawPitchRoll(float pitch, float yaw, float roll, bool isDegree)
		{
			if (isDegree)
			{
				pitch *= Mathf.Deg2Rad;
				yaw *= Mathf.Deg2Rad;
				roll *= Mathf.Deg2Rad;
			}
			// Abbreviations for the various angular functions
			float cy = Mathf.Cos(roll * 0.5f);
			float sy = Mathf.Sin(roll * 0.5f);
			float cp = Mathf.Cos(yaw * 0.5f);
			float sp = Mathf.Sin(yaw * 0.5f);
			float cr = Mathf.Cos(pitch * 0.5f);
			float sr = Mathf.Sin(pitch * 0.5f);

			Quaternion q;
			q.w = cy * cp * cr + sy * sp * sr;
			q.x = cy * cp * sr - sy * sp * cr;
			q.y = sy * cp * sr + cy * sp * cr;
			q.z = sy * cp * cr - cy * sp * sr;
			return q;
		}

		public static bool Approximately(this Quaternion self, Quaternion target)
		{
			return self.GetForward().Approximately(target.GetForward()) &&
				self.GetUp().Approximately(target.GetUp());
		}

		public static bool EqualRoughly(this Quaternion self, Quaternion target, float threshold = float.Epsilon)
		{
			return self.GetForward().EqualRoughly(target.GetForward(), threshold) &&
				self.GetUp().EqualRoughly(target.GetUp(), threshold);
		}

		public static Quaternion WeightedAverage(ReadOnlySpan<Quaternion> qs, ReadOnlySpan<float> ws)
		{
			if (qs == null)	throw new System.NullReferenceException(nameof(qs));
			if (ws == null)	throw new System.NullReferenceException(nameof(ws));
			if (qs.Length != ws.Length) throw new System.ArgumentException($"{nameof(qs)} & {nameof(ws)} lists must be the same length");
			if (qs.Length == 0)
				return Quaternion.identity;
			if (qs.Length == 1)
				return qs[0];

			var average = qs[0];
			var totalWeight = ws[0];
			for (int i = 1; i < qs.Length; ++i)
			{
				totalWeight += ws[i];
				var t = ws[i] / totalWeight;
				average = Quaternion.SlerpUnclamped(average, qs[i], t);
			}
			return average;
		}

		/// <summary>A quick dirty way to rougly calcualte the average,
		/// this method aren't mathematically correct,
		/// since input rotation in different will give you different result.</summary>
		/// <param name="qArray"></param>
		/// <returns></returns>
		public static Quaternion AverageQuaternion(ReadOnlySpan<Quaternion> qArray)
        {
            Quaternion qAvg = qArray[0];
            float weight;
            for (int i = 1; i < qArray.Length; ++i)
            {
                weight = 1.0f / (float)(i + 1);
                qAvg = Quaternion.Slerp(qAvg, qArray[i], weight);
            }
            return qAvg;
        }

        /// <summary>
        /// Get an average (mean) from more then two quaternions (with two, slerp would be used).
        /// Note: this only works if all the quaternions are relatively close together.
        /// <see cref="https://forum.unity.com/threads/average-quaternions.86898/"/>
        /// <see cref="https://stackoverflow.com/questions/12374087/average-of-multiple-quaternions"/>
        /// <seealso cref="http://www.acsu.buffalo.edu/~johnc/ave_quat07.pdf"/>
        /// </summary>
        /// <param name="cumulative">is an external Vector4 which holds all the added x y z and w components.</param>
        /// <param name="newRotation">is the next rotation to be added to the average pool</param>
        /// <param name="firstRotation">is the first quaternion of the array to be averaged</param>
        /// <param name="addAmount">holds the total amount of quaternions which are currently added</param>
        /// <returns>This function returns the current average quaternion</returns>
        public static Quaternion AverageQuaternion(ref Vector4 cumulative,
			Quaternion newRotation,
			Quaternion firstRotation,
			int addAmount)
        {
            float w = 0.0f;
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;

            //Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
            //q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
            if (!AreQuaternionsClose(newRotation, firstRotation))
            {
                newRotation = InverseSignQuaternion(newRotation);
            }

            //Average the values
            float addDet = 1f / (float)addAmount;
            cumulative.w += newRotation.w; w = cumulative.w * addDet;
            cumulative.x += newRotation.x; x = cumulative.x * addDet;
            cumulative.y += newRotation.y; y = cumulative.y * addDet;
            cumulative.z += newRotation.z; z = cumulative.z * addDet;

            //note: if speed is an issue, you can skip the normalization step
            return NormalizeQuaternion(x, y, z, w);
        }

		public static Quaternion AverageQuaternion(IList<Quaternion> multipleRotations)
        {
			float x = 0f, y = 0f, z = 0f, w = 0f;
			Quaternion cumulative = Quaternion.identity;
			for (int i = 0; i < multipleRotations.Count; ++i)
            {
				Quaternion q = multipleRotations[i];

				float denominator = 1.0f / (float)(i + 1f);
				cumulative.x += q.x; x = cumulative.x * denominator;
				cumulative.y += q.y; y = cumulative.y * denominator;
				cumulative.z += q.z; z = cumulative.z * denominator;
				cumulative.w += q.w; w = cumulative.w * denominator;

                //Normalize. Note: experiment to see whether you
                //can skip this step.
				/*
                float D = 1.0f / (w * w + x * x + y * y + z * z);
                w *= D;
                x *= D;
                y *= D;
                z *= D;
				//*/
            }

            //The result is valid right away, without
            //first going through the entire array.
			return NormalizeQuaternion(x, y, z, w);
        }

        public static Quaternion NormalizeQuaternion(float x, float y, float z, float w)
        {
            float lengthD = 1.0f / (w * w + x * x + y * y + z * z);
            w *= lengthD;
            x *= lengthD;
            y *= lengthD;
            z *= lengthD;
            return new Quaternion(x, y, z, w);
        }

        //Changes the sign of the quaternion components. This is not the same as the inverse.
        public static Quaternion InverseSignQuaternion(Quaternion q)
        {
            return new Quaternion(-q.x, -q.y, -q.z, -q.w);
        }

        //Returns true if the two input quaternions are close to each other. This can
        //be used to check whether or not one of two quaternions which are supposed to
        //be very similar but has its component signs reversed (q has the same rotation as
        //-q)
        public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2)
        {
            float dot = Quaternion.Dot(q1, q2);
            if (dot < 0.0f)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
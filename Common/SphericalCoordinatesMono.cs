using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Kit2
{
	/// <summary>
	/// Spherical coordinates.
	/// </summary>
	/// <see cref="http://wiki.unity3d.com/index.php/SphericalCoordinates"/>
	[RequireComponent(typeof(Transform))]
    [ExecuteInEditMode]
    public class SphericalCoordinatesMono : MonoBehaviour
    {
		/// <summary>
		/// the radial distance of that point from a fixed origin.
		/// Radius must be >= 0
		/// </summary>
		public float radius;

		/// <summary>
		/// azimuth angle (in radian) of its orthogonal projection on 
		/// a reference plane that passes through the origin and is orthogonal to the zenith
		/// </summary>
		public float polar;
		public float polarOffset;

		/// <summary>
		/// elevation angle (in radian) from the reference plane 
		/// </summary>
		public float elevation;
		public float elevationOffset;
		
		[Header("Angle Behaviour")]
		public AngleSetup polarSetup = AngleSetup.Default;
		public AngleSetup elevationSetup = AngleSetup.Default;

		[Header("Optional")]
		public Vector3 posOffset = Vector3.zero;
		public Vector3 angularOffset = Vector3.zero;

		[System.Serializable]
		public struct AngleSetup
		{
			public static readonly AngleSetup Default = new AngleSetup
			{
				apply = false,
				loop = false,
				range = new Vector2(0f, 360f),
			};
			public bool apply;
			public bool loop;
			[MinMaxSlider(0f, 360f)] public Vector2 range;
		}

		private void Update()
		{
			if (elevationSetup.apply)
			{
				if (elevation < elevationSetup.range.x || elevation > elevationSetup.range.y)
				{
					var o = elevationSetup;
					var min = Mathf.Min(o.range.x, o.range.y);
					var max = Mathf.Max(o.range.x, o.range.y);
					elevation = o.loop ?
						Mathf.Repeat(elevation, max - min) + min :
						Mathf.Clamp(elevation, o.range.x, o.range.y);
				}
			}

			if (polarSetup.apply)
			{
				if (polar < polarSetup.range.x || polar > polarSetup.range.y)
				{
					var o = polarSetup;
					var min = Mathf.Min(o.range.x, o.range.y);
					var max = Mathf.Max(o.range.x, o.range.y);
					polar = o.loop ?
						Mathf.Repeat(polar, max - min) + min :
						Mathf.Clamp(polar, o.range.x, o.range.y);
				}
			}

			ToCartesian(out var localPos, out var localRot);

			var lp = posOffset + localPos;
			localRot *= Quaternion.Euler(angularOffset);
			transform.SetLocalPositionAndRotation(lp, localRot);
		}

		/// <summary>
		/// Converts a point from Spherical coordinates to Cartesian (using positive * Y as up)
		/// </summary>
		/// <see cref="http://en.wikipedia.org/wiki/Spherical_coordinate_system"/>
		/// <seealso cref="http://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/"/>
		public void ToCartesian(out Vector3 localPos, out Quaternion localRot)
		{
			float p = (polar + polarOffset) * Mathf.Deg2Rad;
			float e = (elevation + elevationOffset) * Mathf.Deg2Rad;
			localPos = new Vector3(
				radius * Mathf.Cos(e) * Mathf.Sin(p), // x
				radius * Mathf.Sin(e),
				radius * Mathf.Cos(e) * Mathf.Cos(p)
			);

			localRot = ToPolarRotation() *
				ToElevationRotation();
		}


		/// <summary>
		/// Convert from cartesian coordinates to spherical coordinates.
		/// </summary>
		/// <see cref="http://en.wikipedia.org/wiki/Spherical_coordinate_system"/>
		/// <seealso cref="http://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/"/>
		public SphericalCoordinatesMono FromCartesian(Vector3 cartesianCoordinate)
		{
			if (cartesianCoordinate.x == 0f)
				cartesianCoordinate.x = Mathf.Epsilon;
			radius = cartesianCoordinate.magnitude;
			var pr = Mathf.Atan(cartesianCoordinate.z / cartesianCoordinate.x);
			if (cartesianCoordinate.x < 0f)
				pr += Mathf.PI;
			
			elevation = Mathf.Asin(cartesianCoordinate.y / radius) * Mathf.Rad2Deg;
			polar = pr * Mathf.Rad2Deg;
			return this;
		}
		public Quaternion ToLocalRotation()
		{
			return
				ToPolarRotation() *
				ToElevationRotation();
		}
		public Quaternion ToPolarRotation() => Quaternion.AngleAxis((polar + polarOffset), Vector3.up);
		public Quaternion ToElevationRotation() => Quaternion.AngleAxis((elevation + elevationOffset), Vector3.left);

		public override string ToString()
		{
			return string.Format($"[Spherical Coordinates Radius={radius:F2}, polar={polar:F2}, elevation={elevation:F2}]");
		}

	}
}

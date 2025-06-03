using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Kit2
{
    public static class PlayableDirectorExtend
    {
		private static void BindTrackAsset(
			PlayableDirector director,
			System.Func<TrackAsset, bool> filterFunc,
			Object value)
		{
			if (director.playableAsset == null)
				throw new System.NullReferenceException();

			var timeline = director.playableAsset as TimelineAsset;
			foreach (var track in timeline.GetOutputTracks())
			{
				bool select = filterFunc == null || filterFunc.Invoke(track);
				if (!select)
					continue;

				director.SetGenericBinding(track, value);
			}
		}

		private static bool FilterTrackByName<TTrack>(TrackAsset trackAsset, string trackName)
			where TTrack : TrackAsset
		{
			if (trackAsset is not TTrack _track)
				return false;

			return _track.name.Equals(trackName, System.StringComparison.InvariantCultureIgnoreCase);
		}

		public static void BindObjectToTrack<TTrack>(this PlayableDirector director, string trackName, Object value)
			where TTrack : TrackAsset
		{
			BindTrackAsset(director, o => FilterTrackByName<TTrack>(o, trackName), value);
		}

		public static void BindObjectToAnimationTrack(this PlayableDirector director, string trackName, Object value)
		{
			BindTrackAsset(director, o => FilterTrackByName<AnimationTrack>(o, trackName), value);
		}

		public static void BindObjectToActivationTrack(this PlayableDirector director, string trackName, Object value)
		{
			BindTrackAsset(director, o => FilterTrackByName<ActivationTrack>(o, trackName), value);
		}

		public static void BindObjectToControlClip(this PlayableDirector director, string trackName, string clipName, Object value)
		{
			var timeline = director.playableAsset as TimelineAsset;
			foreach (var controlTrack in timeline.GetOutputTracks())
			{
				if (controlTrack is not ControlTrack)
					continue;
				if (!controlTrack.name.Equals(trackName))
					continue;

				foreach (var clip in controlTrack.GetClips())
				{
					if (!clip.displayName.Equals(clipName))
						continue;

					if (clip.asset is not ControlPlayableAsset controlClip)
						continue;

					var id = controlClip.sourceGameObject.exposedName;
					director.SetReferenceValue(id, value);
				}
			}
		}

		public static void ClearTrackBinding(this PlayableDirector director, string trackName)
		{
			BindObjectToTrack<TrackAsset>(director, trackName, null);
		}

		public static void UnbindObjectFromTracks(this PlayableDirector director, Object target)
		{
			var timeline = director.playableAsset as TimelineAsset;
			foreach (var track in timeline.GetOutputTracks())
			{
				var currentBinding = director.GetGenericBinding(track);
				if (currentBinding == target)
				{
					director.SetGenericBinding(track, null);
				}
			}
		}

		public static bool ReplaceControlClipBinding(this PlayableDirector director, ControlTrack controlTrack, string clipName, Object newBinding, out Object oldBinding)
		{
			foreach (var clip in controlTrack.GetClips())
			{
				if (clip.asset is not ControlPlayableAsset controlClip)
					continue;

				if (clip.displayName != clipName)
					continue;

				var id = controlClip.sourceGameObject.exposedName;
				oldBinding = director.GetReferenceValue(id, out var isValid);
				director.SetReferenceValue(id, newBinding);
				return oldBinding != null;
			}
			oldBinding = null;
			return false;
		}

		public static bool ReplaceTrackBinding<TTrack>(this PlayableDirector director, GroupTrack rootGroup, string trackName, Object newBinding, out Object oldBinding) where TTrack : TrackAsset
		{
			foreach (var track in rootGroup.GetOutputTracks<TTrack>(trackName))
			{
				oldBinding = director.GetGenericBinding(track);
				director.SetGenericBinding(track, newBinding);
				return oldBinding != null;
			}

			oldBinding = null;
			return false;
		}

		public static bool TryGetTrackBinding<TTrack>(this PlayableDirector director, GroupTrack rootGroup, string trackName, out Object binding) where TTrack : TrackAsset
		{
			foreach (var track in rootGroup.GetChildTracks())
			{
				if (track is GroupTrack groupTrack)
				{
					if (director.TryGetTrackBinding<TTrack>(groupTrack, trackName, out binding))
					{
						return true;
					}
				}
				else
				{
					if (track is not TTrack)
						continue;

					if (track.name != trackName)
						continue;

					binding = director.GetGenericBinding(track);
					return binding != null;
				}
			}
			binding = null;
			return false;
		}

		public static IEnumerable<GroupTrack> GetRootTrackGroups(this TimelineAsset timeline)
		{
			foreach (var track in timeline.GetRootTracks())
			{
				if (track is not GroupTrack groupTrack)
					continue;
				yield return groupTrack;
			}
		}

		public static bool TryGetRootTrackGroup(this TimelineAsset timeline, string groupName, out GroupTrack result)
		{
			foreach (var grp in timeline.GetRootTrackGroups())
			{
				if (!grp.name.Equals(groupName, System.StringComparison.InvariantCultureIgnoreCase))
					continue;
				result = grp;
				return result != null;
			}
			result = null;
			return false;
		}

		public static void MuteRootTrackGroup(this TimelineAsset timeline, string trackGroupName)
		{
			if (timeline.TryGetRootTrackGroup(trackGroupName, out var grp))
				grp.muted = true;
		}

		public static void UnmuteRootTrackGroup(this TimelineAsset timeline, string trackGroupName)
		{
			if (timeline.TryGetRootTrackGroup(trackGroupName, out var grp))
				grp.muted = false;
		}

		public static void MuteTrack(this TimelineAsset timeline, string trackName, System.StringComparison stringComparison = System.StringComparison.InvariantCultureIgnoreCase)
		{
			foreach (var track in timeline.GetOutputTracks())
			{
				if (!track.name.Equals(trackName, stringComparison))
					continue;
				track.muted = true;
				return;
			}
		}

		public static void UnmuteTrack(this TimelineAsset timeline, string trackName, System.StringComparison stringComparison = System.StringComparison.InvariantCultureIgnoreCase)
		{
			foreach (var track in timeline.GetOutputTracks())
			{
				if (!track.name.Equals(trackName, stringComparison))
					continue;
				track.muted = false;
				return;
			}
		}

		public static bool TryGetOutputTrack<TTrack>(this GroupTrack rootGroup, string trackName, out TTrack result) where TTrack : TrackAsset
		{
			foreach (var track in rootGroup.GetOutputTracks<TTrack>(trackName))
			{
				result = track;
				return true;
			}
			result = null;
			return false;
		}

		public static IEnumerable<TTrack> GetOutputTracks<TTrack>(this GroupTrack rootGroup, string trackName) where TTrack : TrackAsset
		{
			foreach (var track in rootGroup.GetChildTracks())
			{
				if (track is GroupTrack groupTrack)
				{
					foreach (var internalTrack in groupTrack.GetOutputTracks<TTrack>(trackName))
					{
						yield return internalTrack;
					}
				}
				else
				{
					if (track is not TTrack typeTrack)
						continue;

					if (track.name != trackName)
						continue;

					yield return typeTrack;
				}
			}
		}


	}
}

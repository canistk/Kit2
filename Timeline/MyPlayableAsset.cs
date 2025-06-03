using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
namespace Kit2.Timeline
{
    public abstract class MyPlayableAsset : PlayableAsset
    {
		/// <example>
		///public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		///{
		///	var playable = ScriptPlayable<MyPlayableBehaviour>.Create(graph);
		///	var behaviour = playable.GetBehaviour();
		///	try
		///	{
		///		// TODO: bind behaviour & your script.
		///	}
		///	catch(System.Exception ex)
		///	{
		///		Debug.LogException(ex);
		///	}
		///	return playable;
		///}
		/// </example>
	}

	public abstract class MyPlayableBehaviour : PlayableBehaviour
    {
	}
}
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
public class CommandMeta {
	public long updated;
}
public enum CommandType { WIGGLE }
[Serializable]
public class CommandData {
	public string uuid;
	public CommandType type;
	public int num;
	public CommandMeta meta;
}
[Serializable]
public class CommandDataContainer {
	public long time;
	public List<CommandData> commands;
}

public class CommandReader : Manager<CommandReader> {

    public string BaseUrl = @"https://inlight.fool.games/";

    [Range(0, 3)]
    public float PollInterval = 1f;
    long latestPollTime;

    void Start() {
        StartCoroutine(Poll());
    }

    IEnumerator Poll() {
        while (true) {
            var req = new WWW(string.Format("{0}command/{1}", BaseUrl, latestPollTime));
            yield return req;
			var clearReqs = new List<WWW>(10);
			if (req.error == null) {
				var firstLoad = latestPollTime == 0;
				var data = JsonUtility.FromJson<CommandDataContainer>(req.text);
				latestPollTime = data.time;
				if (!firstLoad) { // wait until 2nd poll so we know we are pulling using the correct time
					// process commands
					foreach (var command in data.commands) {
						if (command.num > 0) {
							// do command
							switch (command.type) {
								case CommandType.WIGGLE:
									Debug.LogError("WIGGLING " + command.uuid);
									break;
							}
							// clear command
							clearReqs.Add(new WWW(string.Format("{0}command/{1}/{2}/clear", BaseUrl, command.type, command.uuid)));
						}
					}
					// wait for clears
					foreach (var clearReq in clearReqs) {
						yield return clearReq;
					}
					clearReqs.Clear();
				}
			} else {
				Debug.LogWarning(req.error);
			}
			yield return new WaitForSeconds(PollInterval);
        }
    }
}
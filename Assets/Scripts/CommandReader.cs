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

	[Range(0, 20)]
	public float WiggleTime = 5f;
	ListDict<ImageRecord, float> wiggleTimes = new ListDict<ImageRecord, float>();

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
									var record = ImageReader.Inst.Records.Find(command.uuid, (r, uuid) => r.Name == uuid);
									if (record != null) {
										foreach (var recordObj in HasImageRecord.AllInCurrentScene) {
											if (recordObj.Record == record) {
												var spawned = recordObj.GetComponent<SpawnedObject>();
												spawned.IsWiggling = true;
												spawned.Update();
											}
										}
										wiggleTimes[record] = WiggleTime;
									}
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

	void Update() {
		for (int i = 0; i < wiggleTimes.Count; i++) {
			wiggleTimes.Values[i] -= Time.deltaTime;
			if (wiggleTimes.Values[i] <= 0) {
				var record = wiggleTimes.Keys[i];
				foreach (var recordObj in HasImageRecord.AllInCurrentScene) {
					if (recordObj.Record == record) {
						var spawned = recordObj.GetComponent<SpawnedObject>();
						spawned.IsWiggling = false;
						spawned.Update();
					}
				}
				wiggleTimes.RemoveAt(i);
				i--;
			}
		}
	}
}
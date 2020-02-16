using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

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

    public string BaseUrl = @"https://lightfield.fool.games/";

    [Range(0, 3)]
    public float PollInterval = 1f;
    long latestPollTime;

	[Range(0, 20)]
	public float WiggleTime = 5f;
	readonly ListDict<ImageRecord, float> wiggleTimes = new ListDict<ImageRecord, float>();

    void Start() {
        StartCoroutine(Poll());
    }

    IEnumerator Poll() {
        while (true) {
			var req = UnityWebRequest.Get(string.Format("{0}command/{1}", BaseUrl, latestPollTime));
            yield return req.SendWebRequest();
			var clearReqs = new List<UnityWebRequest>(10);
			if (req.error == null) {
				var firstLoad = latestPollTime == 0;
				var data = JsonUtility.FromJson<CommandDataContainer>(req.downloadHandler.text);
				latestPollTime = data.time;
				if (!firstLoad) { // wait until 2nd poll so we know we are pulling using the correct time
					// process commands
					foreach (var command in data.commands) {
						// do command
						switch (command.type) {
							case CommandType.WIGGLE:
								Debug.LogError("WIGGLING " + command.uuid);
								var record = ImageReader.Inst.Records.Find(command.uuid, (r, uuid) => r.Name == uuid);
								if (record != null) {
									foreach (var obj in SpawnedObject.AllInCurrentScene) {
										if (obj.Record.Record == record) {
											obj.IsWiggling = true;
											obj.Update();
										}
									}
									wiggleTimes[record] = WiggleTime;
								}
								break;
						}
						// clear command
						clearReqs.Add(UnityWebRequest.Get(string.Format("{0}command/{1}/{2}/clear", BaseUrl, command.type, command.uuid)));
					}
					// wait for clears
					foreach (var clearReq in clearReqs) {
						yield return clearReq.SendWebRequest();
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
				foreach (var obj in SpawnedObject.AllInCurrentScene) {
					if (obj.Record.Record == record) {
						obj.IsWiggling = false;
						obj.Update();
					}
				}
				wiggleTimes.RemoveAt(i);
				i--;
			}
		}
	}
}
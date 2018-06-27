import os
import threading
import requests

# config
URL_INDEX = 'https://inlight.fool.games/drawingindex/'
URL_IMG = 'https://inlight.fool.games/img/drawings/approved/'
INTERVAL = 1.5

# lib
def blockingInterval(func, interval):
    func()
    e = threading.Event()
    while not e.wait(interval):
        func()

# main
lastUpdateTime = 0
def pullIndex():
    global lastUpdateTime
    res = requests.get(URL_INDEX + str(lastUpdateTime))
    if res.status_code == 200:
        json = res.json()
        time = json[u'time']
        changes = json[u'changes']
        print('SUCCESS: ' + str(res.status_code) + ': ' + str(len(changes)) + ' changes at ' + str(lastUpdateTime))
        lastUpdateTime = time

        if len(changes) > 0:
            # read index
            indexText = ''
            with open('./index.txt', 'r') as indexFile:
                indexText = indexFile.read()
            index = list(map(lambda line: line.split(' '), filter(str.strip, indexText.splitlines())))
            # process changes
            uuidsToRemove = []
            for change in changes:
                print(change)
                uuid = change[u'uuid']
                drawingType = change[u'type']
                drawingFacing = change[u'facing']
                if change[u'deleted']:
                    # remove from index
                    uuidsToRemove.append(uuid)
                    # delete file
                    try:
                        os.remove('./' + uuid + '.png')
                    except OSError:
                        pass
                elif change[u'completed']:
                    # download file
                    imgRes = requests.get(URL_IMG + str(uuid) + '.png')
                    if imgRes.status_code == 200:
                        # add to index
                        updated = False
                        for pair in index:
                            if pair[0] == uuid:
                                pair[1] = drawingType
                                pair[2] = drawingFacing
                                updated = True
                        if not updated:
                            index.append([uuid, drawingType, drawingFacing])
                        print(URL_IMG + str(uuid) + '.png')
                        # save
                        with open('./' + uuid + '.png', 'wb+') as imgFile:
                            imgFile.write(imgRes.content)
            # remove uuids from index
            index = [t for t in index if t[0] not in uuidsToRemove]
            # save index
            with open('./index.txt', 'w') as indexFile:
                indexFile.write('\n'.join(map(lambda pair: str(pair[0]) + ' ' + str(pair[1]) + ' ' + str(pair[2]), index)))
    else:
        print('ERROR: ' + str(res.status_code) + ': at ' + str(lastUpdateTime))

# init
with open('./index.txt', 'w+') as indexFile:
    indexFile.write('')
blockingInterval(pullIndex, INTERVAL)

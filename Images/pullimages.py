import os
import threading
import requests

# config
URL_INDEX = 'http://34.206.49.42:8000/drawingindex/'
URL_IMG = 'http://34.206.49.42:8000/img/drawings/approved/'
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
        print 'SUCCESS: ' + str(res.status_code) + ': ' + str(len(changes)) + ' changes ' + ' at ' + str(lastUpdateTime)
        lastUpdateTime = time

        if len(changes) > 0:
            # read index
            indexText = ''
            with open('./index.txt', 'r') as indexFile:
                indexText = indexFile.read()
            index = filter(str.strip, indexText.splitlines())
            # process changes
            for change in changes:
                print change
                uuid = change[u'uuid']
                if change[u'deleted']:
                    # remove from index
                    if uuid in index:
                        index.remove(uuid)
                    # delete file
                    try:
                        os.remove('./' + uuid + '.png')
                    except OSError:
                        pass
                else:
                    # download file
                    imgRes = requests.get(URL_IMG + str(uuid) + '.png')
                    if imgRes.status_code == 200:
                        # add to index
                        if uuid not in index:
                            index.append(uuid)
                        print URL_IMG + str(uuid) + '.png'
                        # save
                        with open('./' + uuid + '.png', 'wb+') as imgFile:
                            imgFile.write(imgRes.content)
            # save index
            with open('./index.txt', 'w') as indexFile:
                indexFile.write('\n'.join(index))
    else:
        print 'ERROR: ' + str(res.status_code) + ': at ' + str(lastUpdateTime)

# init
with open('./index.txt', 'w+') as indexFile:
    indexFile.write('')
blockingInterval(pullIndex, INTERVAL)
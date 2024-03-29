import requests
import json
import os
script_path = os.path.abspath(__file__)
print("Current working directory:", os.getcwd())
script_dir = os.path.dirname(script_path)
os.chdir(script_dir)

# 本地JSON文件路径
skins_path = r'./website/data/skins.json'
gloves_path = './website/data/gloves.json'
agents_path = r'./website/data/agents.json'
music_path = r'./website/data/music.json'
#Can be one of the following: bg cs da de el en es-ES es-MX fi fr hu it ja ko nl no pl pt-BR pt-PT ro ru sk sv th tr uk zh-CN zh-TW vi
target_language = 'zh-CN' 

# 请求在线JSON数据
skin_url = f"https://bymykel.github.io/CSGO-API/api/{target_language}/skins.json"
response = requests.get(skin_url)
skins_online = response.json()


def GetFromIndex(local_path):
    with open(local_path, 'r', encoding='utf-8') as file:
        skins = json.load(file)
    paint_name_mapping = {skin['paint_index']: skin['name'] for skin in skins_online}
    #print(paint_name_mapping)
    print('begin to search')
    for skin in skins:
        print(skin)
        paint = skin.get("paint")
        if paint in paint_name_mapping:
            skin["paint_name"] = paint_name_mapping[paint]
    with open(local_path, 'w', encoding='utf-8') as file:
        json.dump(skins, file, ensure_ascii=False, indent=4)
    print('Translate:'+local_path+'to'+target_language)
    
GetFromIndex(gloves_path)
GetFromIndex(skins_path)

def Agents():
    agents_url = f"https://bymykel.github.io/CSGO-API/api/{target_language}/agents.json"
    response = requests.get(agents_url)
    agents_online = response.json()
    with open(agents_path, 'r', encoding='utf-8') as file:
        agents = json.load(file)
    paint_name_mapping = {agent['market_hash_name']: agent['name'] for agent in agents_online}
    #print(paint_name_mapping)
    print('begin to search')
    for agent in agents:
        print(agent)
        paint = agent.get("agent_name")
        if paint in paint_name_mapping:
            agent["agent_name"] = paint_name_mapping[paint]
    with open(agents_path, 'w', encoding='utf-8') as file:
        json.dump(agents, file, ensure_ascii=False, indent=4)
    print('Translate:'+agents_path+' to '+target_language)
    

def Music():
    music_url = f"https://bymykel.github.io/CSGO-API/api/{target_language}/music_kits.json"
    response = requests.get(music_url)
    music_online = response.json()

    with open(music_path, 'r', encoding='utf-8') as file:
        musicKits = json.load(file)

    paint_name_mapping = {music['id'].split('-')[1]: music['name'] for music in music_online}
    print(paint_name_mapping)
    print('begin to search')

    for music_item in musicKits:
        print(music_item)
        paint = music_item.get("id")
        if paint in paint_name_mapping:
            music_item["name"] = paint_name_mapping[paint]

    with open(music_path, 'w', encoding='utf-8') as file:
        json.dump(musicKits, file, ensure_ascii=False, indent=4)

    print('Translate:'+music_path+' to '+target_language)

Agents()
Music()
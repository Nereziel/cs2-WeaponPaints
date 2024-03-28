import requests
import random
import json
from hashlib import md5
import urllib.parse
import re
import html
import concurrent.futures
import os

# setup the JSON file path
json_file_path = r'' 
# setup  target language
language='zh-CN'

class BaiduTranslate:
    # For list of language codes, please refer to `https://api.fanyi.baidu.com/doc/21`
    def __init__(self):
        self.appid = ""  #*must*   you can apply in https://api.fanyi.baidu.com/
        self.appkey = "" #*must*   you can apply in https://api.fanyi.baidu.com/ 
        
        
        self.endpoint = "http://api.fanyi.baidu.com"
        self.path = "/api/trans/vip/translate"
        self.url = self.endpoint + self.path
    
    def make_md5(self, s, encoding="utf-8"):
        return md5(s.encode(encoding)).hexdigest()

    def baidu_api(self, query):
        salt = random.randint(32768, 65536)
        sign = self.make_md5(self.appid + query + str(salt) + self.appkey)

        headers = {"Content-Type": "application/x-www-form-urlencoded"}
        payload = {
            "appid": self.appid,
            "q": query,
            "from": "auto",
            "to": f"{language}",
            "salt": salt,
            "sign": sign,
        }

        r = requests.post(self.url, data=payload, headers=headers)  
        result = r.json()

        print("API Response:", result)

        return result["trans_result"][0]["dst"] if 'trans_result' in result else query

class GoogleTranslate:
    def __init__(self, source_language='auto', target_language=language, timeout=5):
        self.source_language = source_language
        self.target_language = target_language
        self.timeout = timeout
        self.pattern = r'(?s)class="(?:t0|result-container)">(.*?)<'

    def make_request(self, target_language, source_language, text, timeout):
        escaped_text = urllib.parse.quote(text)
        url = f'https://translate.google.com/m?tl={target_language}&sl={source_language}&q={escaped_text}'
        response = requests.get(url, timeout=timeout)
        result = re.findall(self.pattern, response.text)
        if not result:
            print('\nError: Unknown error.')
            with open('error.txt', 'w') as f:
                f.write(response.text)
            exit(0)
        return html.unescape(result[0])

    def translate(self, text, target_language='', source_language='', timeout=''):
        if not target_language:
            target_language = self.target_language
        if not source_language:
            source_language = self.source_language
        if not timeout:
            timeout = self.timeout
        if len(text) > 5000:
            print('\nError: It can only detect 5000 characters at once. (%d characters found.)' % (len(text)))
            exit(0)
        if isinstance(target_language, list):
            with concurrent.futures.ThreadPoolExecutor() as executor:
                futures = [executor.submit(self.make_request, target, source_language, text, timeout) for target in target_language]
                return [f.result() for f in futures]
        return self.make_request(target_language, source_language, text, timeout)

    def translate_file(self, file_path, target_language='', source_language='', timeout=''):
        if not os.path.isfile(file_path):
            print('\nError: The file or path is incorrect.')
            exit(0)
        with open(file_path, 'r', encoding='utf-8') as f:
            text = f.read()
        return self.translate(text, target_language, source_language, timeout)
    
#choise menu
service_choice = input("Choose translation service (baidu/google): ")

if service_choice.lower() == 'baidu':
    translator = BaiduTranslate()
elif service_choice.lower() == 'google':
    translator = GoogleTranslate()
else:
    print("Invalid choice. Exiting.")
    exit()

def translate_text(text, translator):
    if isinstance(translator, BaiduTranslate):
        return translator.baidu_api(text)
    elif isinstance(translator, GoogleTranslate):
        return translator.translate(text)
    else:
        return text


# read `skins.json` file
with open(json_file_path, 'r', encoding='utf-8') as f:
    skins_data = json.load(f)

#translate `paint_name`
for i, skin in enumerate(skins_data):
    english_text = skin['paint_name']
    # translate and update `paint_name`
    skins_data[i]['paint_name'] = translate_text(english_text, translator)
    
    # Write updated list back to JSON file
    with open(json_file_path, 'w', encoding='utf-8') as f_out:
        json.dump(skins_data, f_out, ensure_ascii=False, indent=4)

print("Translation completed and file updated.")
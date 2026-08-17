import json

with open('C:/Users/Irah/Documents/Unity Projects/Luminang_New/Assets/Resources/LuminangPhrases.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

for phrase in data['phrases']:
    if phrase['category'] == 'Linking Verbs':
        print(f"{phrase['id']}: {phrase['english']} ({phrase['ilokano']} | {phrase['cebuano']})")

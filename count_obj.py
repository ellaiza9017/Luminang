import json

with open('Assets/Data/Cebuano Objectives.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
c = 0
for cat in data['objectives']:
    c += len(cat['items'])
print(f'Total Cebuano: {c}')

with open('Assets/Data/Ilokano Objectives.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
c = 0
for cat in data['objectives']:
    c += len(cat['items'])
print(f'Total Ilokano: {c}')

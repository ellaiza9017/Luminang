import re, json

scene_file = "Assets/Scenes/Environments/Magellan's_Cross.unity"
set_objs = set()

with open(scene_file, 'r', encoding='utf-8') as f:
    for line in f:
        match = re.search(r"m_StringArgument: 'Objective:\s*(.*)'", line)
        if match:
            obj = match.group(1).strip()
            set_objs.add(obj)

json_file = 'Assets/Data/Cebuano Objectives.json'
json_objs = []
with open(json_file, 'r', encoding='utf-8') as f:
    data = json.load(f)
    for cat in data.get('objectives', []):
        for item in cat.get('items', []):
            json_objs.append(item.get('objective'))

print('=== Objectives in JSON but NEVER SET in Magellan Scene ===')
for j_obj in json_objs:
    found = False
    for s_obj in set_objs:
        if j_obj.lower().strip() == s_obj.lower().strip():
            found = True
            break
        if "Magellan's" in j_obj and "Magella's" in s_obj:
            found = True
            break
    if not found:
        print(f'MISSING SET: {j_obj}')
print('\n=== End of Check ===')

import re, json

scene_file = "Assets/Scenes/Environments/Magellan's_Cross.unity"
required_objs = set()

with open(scene_file, 'r', encoding='utf-8') as f:
    for line in f:
        match = re.search(r'requiredObjective:\s*(.*)', line)
        if match:
            obj = match.group(1).strip()
            if obj.startswith('- '):
                obj = obj[2:].strip()
            if obj.startswith('requiredObjective:'):
                obj = obj.replace('requiredObjective:', '').strip()
            if obj.startswith('Objective:'):
                obj = obj.replace('Objective:', '').strip()
            if obj.startswith('- requiredObjective:'):
                obj = obj.replace('- requiredObjective:', '').strip()
            required_objs.add(obj)

json_file = 'Assets/Data/Cebuano Objectives.json'
json_objs = []
with open(json_file, 'r', encoding='utf-8') as f:
    data = json.load(f)
    for cat in data.get('objectives', []):
        for item in cat.get('items', []):
            json_objs.append(item.get('objective'))

print('=== Objectives in SCENE but NOT in Cebuano JSON ===')
for r_obj in required_objs:
    found = False
    for j_obj in json_objs:
        if r_obj.lower().strip() == j_obj.lower().strip() or r_obj.strip() == j_obj.strip() + '.':
            found = True
            break
        if "Magellan's" in j_obj and "Magella's" in r_obj:
            found = True
            break
    if not found:
        print(f'Extra in Scene: {r_obj}')
print('\n=== End of Check ===')

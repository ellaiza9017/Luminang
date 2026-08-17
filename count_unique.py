import re

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
            required_objs.add(obj.lower().replace('.', '').strip())

print(f'Total Unique Objectives on NPCs in Scene: {len(required_objs)}')
for o in sorted(required_objs):
    print(o)

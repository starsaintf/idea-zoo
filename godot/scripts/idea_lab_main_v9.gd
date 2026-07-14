extends "res://scripts/idea_lab_main_v8.gd"

var last_archive_previous_path := ""

func _save_record(record: Dictionary) -> bool:
	var path = "user://idea_zoo_real_ideas.json"
	var archive: Array = []
	var previous_raw := ""
	last_archive_backup_path = ""
	last_archive_previous_path = ""

	if FileAccess.file_exists(path):
		previous_raw = FileAccess.get_file_as_string(path)
		var parser = JSON.new()
		var parse_result = parser.parse(previous_raw)
		if parse_result == OK and parser.data is Array:
			archive = parser.data
			last_archive_previous_path = "user://idea_zoo_real_ideas_previous.json"
			var previous = FileAccess.open(last_archive_previous_path, FileAccess.WRITE)
			if previous != null:
				previous.store_string(previous_raw)
				previous.close()
		elif not previous_raw.strip_edges().is_empty():
			var stamp = int(Time.get_unix_time_from_system() * 1000.0)
			last_archive_backup_path = "user://idea_zoo_real_ideas_corrupt_%d.json" % stamp
			var backup = FileAccess.open(last_archive_backup_path, FileAccess.WRITE)
			if backup != null:
				backup.store_string(previous_raw)
				backup.close()

	var stored = record.duplicate(true)
	var now_ms = int(Time.get_unix_time_from_system() * 1000.0)
	var slug = String(stored.get("title", "idea")).to_lower().replace(" ", "-")
	stored["saved_at"] = Time.get_datetime_string_from_system()
	stored["record_id"] = "%s-%d-%d" % [slug, now_ms, randi_range(1000, 9999)]
	archive.append(stored)
	var serialized = JSON.stringify(archive, "\t")

	var temp_path = path + ".tmp"
	var temp = FileAccess.open(temp_path, FileAccess.WRITE)
	if temp == null:
		return false
	temp.store_string(serialized)
	temp.close()

	var path_absolute = ProjectSettings.globalize_path(path)
	var temp_absolute = ProjectSettings.globalize_path(temp_path)
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(path_absolute)
	if DirAccess.rename_absolute(temp_absolute, path_absolute) == OK:
		return true

	var fallback = FileAccess.open(path, FileAccess.WRITE)
	if fallback == null:
		return false
	fallback.store_string(serialized)
	fallback.close()
	return true

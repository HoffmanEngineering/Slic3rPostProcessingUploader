; HEADER_BLOCK_START
; BambuStudio 01.10.01.50
; estimated printing time (normal mode) = 16m 36s
; total layer number: 100
; total filament length [mm] : 1487.59
; total filament volume [cm^3] : 3578.08
; total filament weight [g] : 4.44
; filament_density: 1.24
; filament_diameter: 1.75
; max_z_height: 20.08
; HEADER_BLOCK_END

; CONFIG_BLOCK_START
; accel_to_decel_enable = 0
; accel_to_decel_factor = 50%
; activate_air_filtration = 0
; additional_cooling_fan_speed = 70
; auxiliary_fan = 0
; bed_custom_model = 
; bed_custom_texture = 
; bed_exclude_area = 0x0
; before_layer_change_gcode = ;BEFORE_LAYER_CHANGE
[layer_num] @ [layer_z]mm
; best_object_pos = 0.5,0.5
; bottom_shell_layers = 3
; bottom_shell_thickness = 1.2
; bottom_surface_pattern = monotonic
; bridge_angle = 0
; bridge_flow = 0.85
; bridge_no_support = 1
; bridge_speed = 40
; brim_object_gap = 0.12
; brim_type = auto_brim
; brim_width = 3
; chamber_temperatures = 0
; change_filament_gcode = M600
; close_fan_the_first_x_layers = 1
; complete_print_exhaust_fan_speed = 80
; cool_plate_temp = 35
; cool_plate_temp_initial_layer = 35
; curr_bed_type = High Temp Plate
; default_acceleration = 2500
; default_filament_colour = ""
; default_filament_profile = "Anycubic Generic PLA"
; default_jerk = 0
; default_print_profile = 0.20mm Standard @Anycubic Kobra2
; deretraction_speed = 80
; detect_narrow_internal_solid_infill = 1
; detect_overhang_wall = 1
; detect_thin_wall = 0
; draft_shield = disabled
; during_print_exhaust_fan_speed = 60
; elefant_foot_compensation = 0.1
; enable_arc_fitting = 0
; enable_long_retraction_when_cut = 0
; enable_overhang_bridge_fan = 1
; enable_overhang_speed = 1
; enable_pressure_advance = 0
; enable_prime_tower = 0
; enable_support = 0
; enforce_support_layers = 0
; eng_plate_temp = 0
; eng_plate_temp_initial_layer = 0
; ensure_vertical_shell_thickness = 1
; exclude_object = 1
; extruder_clearance_dist_to_rod = 33
; extruder_clearance_height_to_lid = 140
; extruder_clearance_height_to_rod = 36
; extruder_clearance_max_radius = 65
; extruder_colour = #018001
; extruder_offset = 0x0
; extruder_type = DirectDrive
; fan_cooling_layer_time = 100
; fan_max_speed = 100
; fan_min_speed = 100
; filament_colour = #00AE42
; filament_cost = 20
; filament_density = 1.24
; filament_diameter = 1.75
; filament_end_gcode = "; filament end gcode 
"
; filament_flow_ratio = 0.98
; filament_ids = GFL99
; filament_is_support = 0
; filament_max_volumetric_speed = 12
; filament_minimal_purge_on_wipe_tower = 15
; filament_notes = 
; filament_scarf_gap = 0
; filament_scarf_height = 0%
; filament_scarf_length = 10
; filament_scarf_seam_type = none
; filament_settings_id = "Generic PLA @Anycubic"
; filament_shrink = 100%
; filament_soluble = 0
; filament_start_gcode = "; filament start gcode
"
; filament_type = PLA
; filament_vendor = Generic
; filename_format = {input_filename_base}_{filament_type[0]}_{layer_height}_{print_time}.gcode
; filter_out_gap_fill = 0
; first_layer_print_sequence = 0
; flush_into_infill = 0
; flush_into_objects = 0
; flush_into_support = 1
; flush_multiplier = 1
; flush_volumes_matrix = 0
; flush_volumes_vector = 140,140
; full_fan_speed_layer = 0
; fuzzy_skin = none
; fuzzy_skin_point_distance = 0.8
; fuzzy_skin_thickness = 0.3
; gap_infill_speed = 100
; gcode_add_line_number = 0
; gcode_flavor = marlin
; has_scarf_joint_seam = 0
; head_wrap_detect_zone = 
; host_type = octoprint
; hot_plate_temp = 45
; hot_plate_temp_initial_layer = 45
; independent_support_layer_height = 1
; infill_combination = 0
; infill_direction = 45
; infill_jerk = 9
; infill_wall_overlap = 15%
; initial_layer_acceleration = 2000
; initial_layer_flow_ratio = 1
; initial_layer_infill_speed = 50
; initial_layer_jerk = 9
; initial_layer_line_width = 0.8
; initial_layer_print_height = 0.28
; initial_layer_speed = 45
; inner_wall_acceleration = 0
; inner_wall_jerk = 9
; inner_wall_line_width = 0.5
; inner_wall_speed = 150
; interface_shells = 0
; internal_bridge_support_thickness = 0
; internal_solid_infill_line_width = 0.5
; internal_solid_infill_pattern = zig-zag
; internal_solid_infill_speed = 150
; ironing_direction = 45
; ironing_flow = 15%
; ironing_inset = 0
; ironing_pattern = zig-zag
; ironing_spacing = 0.1
; ironing_speed = 15
; ironing_type = no ironing
; is_infill_first = 0
; layer_change_gcode = ;AFTER_LAYER_CHANGE
[layer_num] @ [layer_z]mm
; layer_height = 0.2
; line_width = 0.42
; long_retractions_when_cut = 0
; machine_end_gcode = M104 S0 ;Extruder off
M140 S0 ;Heatbed off
M107 ;Fan off
G91 ;Relative positioning
G1 E-5 F3000 ;Retract filament
G1 Z+0.3 F3000 ;Lift print head
G28 X0 F3000 ;Home X axis
M84 ;Disable stepper motors
; machine_load_filament_time = 0
; machine_max_acceleration_e = 5000,5000
; machine_max_acceleration_extruding = 2500,2500
; machine_max_acceleration_retracting = 2500,2500
; machine_max_acceleration_travel = 3000,1250
; machine_max_acceleration_x = 2500,2500
; machine_max_acceleration_y = 2500,2500
; machine_max_acceleration_z = 800,800
; machine_max_jerk_e = 10,10
; machine_max_jerk_x = 15,15
; machine_max_jerk_y = 10,10
; machine_max_jerk_z = 2,2
; machine_max_speed_e = 80,80
; machine_max_speed_x = 300,300
; machine_max_speed_y = 250,250
; machine_max_speed_z = 8,8
; machine_min_extruding_rate = 0,0
; machine_min_travel_rate = 0,0
; machine_pause_gcode = M601
; machine_start_gcode = G90 ;Use absolute coordinates
M83 ;Extruder relative mode
M104 S[first_layer_temperature] ;Set extruder temp
M140 S[first_layer_bed_temperature] ;Set bed temp
M190 S[first_layer_bed_temperature] ;Wait for bed temp
M109 S[first_layer_temperature] ;Wait for extruder temp
G28 ;Move X/Y/Z to min endstops
G1 Z0.28 ;Lift nozzle a bit
G92 E0 ;Specify current extruder position as zero
G1 Y3 F1800 ;Move Y to purge point
G1 X60 E25 F500 ;Extrude 25mm of filament in a 5cm line
G92 E0 ;Zero the extruded length again
G1 E-2 F500 ;Retract a little
G1 X70 F4000 ;Quickly wipe away from the filament line
M117
; machine_unload_filament_time = 0
; max_bridge_length = 10
; max_layer_height = 0.32
; max_travel_detour_distance = 0
; min_bead_width = 85%
; min_feature_size = 25%
; min_layer_height = 0.04
; minimum_sparse_infill_area = 0
; mmu_segmented_region_interlocking_depth = 0
; mmu_segmented_region_max_width = 0
; nozzle_diameter = 0.4
; nozzle_height = 4
; nozzle_temperature = 220
; nozzle_temperature_initial_layer = 220
; nozzle_temperature_range_high = 230
; nozzle_temperature_range_low = 190
; nozzle_type = undefine
; nozzle_volume = 0
; only_one_wall_first_layer = 0
; ooze_prevention = 0
; other_layers_print_sequence = 0
; other_layers_print_sequence_nums = 0
; outer_wall_acceleration = 700
; outer_wall_jerk = 9
; outer_wall_line_width = 0.4
; outer_wall_speed = 150
; overhang_1_4_speed = 0
; overhang_2_4_speed = 20
; overhang_3_4_speed = 15
; overhang_4_4_speed = 10
; overhang_fan_speed = 100
; overhang_fan_threshold = 50%
; overhang_threshold_participating_cooling = 95%
; overhang_totally_speed = 24
; post_process = 
; precise_z_height = 0
; pressure_advance = 0.02
; prime_tower_brim_width = 3
; prime_tower_width = 60
; prime_volume = 45
; print_compatible_printers = "Anycubic Kobra 2 0.4 nozzle"
; print_flow_ratio = 1
; print_sequence = by layer
; print_settings_id = 0.20mm Standard @Anycubic Kobra2
; printable_area = 0x0,220x0,220x220,0x220
; printable_height = 250
; printer_model = Anycubic Kobra 2
; printer_notes = 
; printer_settings_id = Anycubic Kobra 2 0.4 nozzle
; printer_structure = undefine
; printer_technology = FFF
; printer_variant = 0.4
; printhost_authorization_type = key
; printhost_ssl_ignore_revoke = 0
; printing_by_object_gcode = 
; process_notes = 
; raft_contact_distance = 0.1
; raft_expansion = 1.5
; raft_first_layer_density = 90%
; raft_first_layer_expansion = 2
; raft_layers = 0
; reduce_crossing_wall = 0
; reduce_fan_stop_start_freq = 1
; reduce_infill_retraction = 1
; required_nozzle_HRC = 3
; resolution = 0.012
; retract_before_wipe = 70%
; retract_length_toolchange = 1
; retract_lift_above = 0
; retract_lift_below = 249
; retract_restart_extra = 0
; retract_restart_extra_toolchange = 0
; retract_when_changing_layer = 1
; retraction_distances_when_cut = 18
; retraction_length = 2
; retraction_minimum_travel = 1
; retraction_speed = 80
; role_base_wipe_speed = 1
; scan_first_layer = 0
; scarf_angle_threshold = 155
; seam_gap = 15%
; seam_position = aligned
; seam_slope_conditional = 1
; seam_slope_entire_loop = 0
; seam_slope_inner_walls = 1
; seam_slope_steps = 10
; silent_mode = 0
; single_extruder_multi_material = 0
; skirt_distance = 2
; skirt_height = 1
; skirt_loops = 0
; slice_closing_radius = 0.049
; slicing_mode = regular
; slow_down_for_layer_cooling = 1
; slow_down_layer_time = 8
; slow_down_min_speed = 10
; small_perimeter_speed = 50%
; small_perimeter_threshold = 0
; smooth_coefficient = 80
; smooth_speed_discontinuity_area = 1
; solid_infill_filament = 1
; sparse_infill_acceleration = 100%
; sparse_infill_anchor = 400%
; sparse_infill_anchor_max = 20
; sparse_infill_density = 10%
; sparse_infill_filament = 1
; sparse_infill_line_width = 0.45
; sparse_infill_pattern = zig-zag
; sparse_infill_speed = 70
; spiral_mode = 0
; spiral_mode_max_xy_smoothing = 200%
; spiral_mode_smooth = 0
; standby_temperature_delta = -5
; start_end_points = 30x-3,54x245
; supertack_plate_temp = 35
; supertack_plate_temp_initial_layer = 35
; support_air_filtration = 0
; support_angle = 0
; support_base_pattern = default
; support_base_pattern_spacing = 2.5
; support_bottom_interface_spacing = 0.5
; support_bottom_z_distance = 0.2
; support_chamber_temp_control = 0
; support_critical_regions_only = 0
; support_expansion = 0
; support_filament = 0
; support_interface_bottom_layers = 0
; support_interface_filament = 0
; support_interface_loop_pattern = 0
; support_interface_not_for_body = 1
; support_interface_pattern = auto
; support_interface_spacing = 0.5
; support_interface_speed = 100
; support_interface_top_layers = 3
; support_line_width = 0.4
; support_object_first_layer_gap = 0.2
; support_object_xy_distance = 0.35
; support_on_build_plate_only = 0
; support_remove_small_overhang = 1
; support_speed = 100
; support_style = default
; support_threshold_angle = 30
; support_top_z_distance = 0.2
; support_type = normal(auto)
; temperature_vitrification = 60
; template_custom_gcode = 
; textured_plate_temp = 45
; textured_plate_temp_initial_layer = 45
; thick_bridges = 0
; thumbnail_size = 50x50
; time_lapse_gcode = 
; timelapse_type = 0
; top_area_threshold = 100%
; top_one_wall_type = all top
; top_shell_layers = 3
; top_shell_thickness = 0.6
; top_solid_infill_flow_ratio = 1
; top_surface_acceleration = 0
; top_surface_jerk = 9
; top_surface_line_width = 0.45
; top_surface_pattern = monotonic
; top_surface_speed = 120
; travel_jerk = 9
; travel_speed = 120
; travel_speed_z = 0
; tree_support_branch_angle = 40
; tree_support_branch_diameter = 5
; tree_support_branch_diameter_angle = 5
; tree_support_branch_distance = 5
; tree_support_wall_count = 0
; upward_compatible_machine = 
; use_firmware_retraction = 0
; use_relative_e_distances = 1
; wall_distribution_count = 1
; wall_filament = 1
; wall_generator = arachne
; wall_loops = 3
; wall_sequence = inner wall/outer wall
; wall_transition_angle = 10
; wall_transition_filter_deviation = 25%
; wall_transition_length = 100%
; wipe = 1
; wipe_distance = 2
; wipe_speed = 80%
; wipe_tower_no_sparse_layers = 0
; wipe_tower_rotation_angle = 0
; wipe_tower_x = 165
; wipe_tower_y = 250
; xy_contour_compensation = 0.02
; xy_hole_compensation = 0.02
; z_hop = 0.4
; z_hop_types = Normal Lift
; CONFIG_BLOCK_END

; THUMBNAIL_BLOCK_START
; thumbnail begin 50x50 1136
; iVBORw0KGgoAAAANSUhEUgAAADIAAAAyCAYAAAAeP4ixAAADGklEQVR4Ae2Y204TURSG1wWJeCCS2q
; SJGKgtFloQag8cCk24MNx40+iNKFE0TWNUsJ6i0Wg9XFTroRoNUaOBN+gj8Ah9hD5CH2Hsz3bbod3T
; zkxnOjPJ/MkKhNmF9fGv9e+mRK5cubJWRwaGyTvoJ0dr8vgKbS/W9yt2IkOO1JVAmXbT0oHCz+CQIz
; R6NEpvYtU2CF54hjO21upIXhFAXhg1nLWdO2jo6cyeKgh54TW2CYJl34awye8LEn1bUOcOfoelyoZ2
; hM0VohJdG5foZVS9O1uRSv9HDcv6MVlra+b3kkS3wwwCMK/PMWfUwuB39i2mM6MFYRPFBAMACG/+1x
; KD0bI3GDX8DdPc6RSrT84yiOez7c8AAiAtMDwIDI/pTrGKccIYwRGlpS/GtYPIY9oQZcbKupqQF5b+
; z7L42c9U99fnJirUs9aDVSollBtRU1/n212Bkw+nJbrb2Kkv88qvhdsXx2rUs9ZOV2mz8ceezTRmPS
; XRblpfyV0pNxq/GWL1ISk+/yPVTEBDQVD5iLTvjh6Qz3OsEAhobivCXBGd5QnIy3AQXnBH66jh/L0I
; a+xtTPkcT8C+gHB3Os22qN4n2MiIniHdchPtEKaD8MJ/t5cgQPFxU6q+gKAwDtuL2gHgAl9oXtmQhS
; DyIFDrDpxsbfjxNCtLQeRB0Cmm+f3R2uyNMxK9i1s8WiJ3PgnuB35/iJq9P8XuGluB8HoluwS7LTQu
; xmzIpiA8CESxejUo0SU/+/5OWNkN24BshpUbXA/iIyIGcWvSwSB8yUVJ5UgQOLMWYF8dCQKIUrIJg7
; MiGENA5rwF00AeNUbqxb8lL0Sb75JbzyW9ZTJE3sEo5UJ1w0FKssjF/QKY1oTzHV4hw3X+5I5hIHiP
; VYwrQ66O7JGpCgxlNLsjahQjhdtc9Cw8bNAHDt2EUbtwqqIbBMst2gW44DlkwSf1aV9ZFwhu+wdTJi
; 20XsGd6+M1TSDyN46XA3VzFlqvOgWBZQutV0pBIIIIDm2QrYVRa3Wn1YVjA35yjKKe/H93OMSsp0CO
; FA8C2y20K1euHKu/gG1NvibXFfQAAAAASUVORK5CYII=
; thumbnail end
; THUMBNAIL_BLOCK_END
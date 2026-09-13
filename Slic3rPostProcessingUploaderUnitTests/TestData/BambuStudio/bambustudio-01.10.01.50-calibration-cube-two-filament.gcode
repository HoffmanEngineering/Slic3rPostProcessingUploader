            ; HEADER_BLOCK_START
; BambuStudio 01.10.01.50
; model printing time: 4h 6m 55s; total estimated time: 4h 12m 53s
; total layer number: 128
; total filament length [mm] : 9035.47,11467.79
; total filament volume [cm^3] : 21732.84,27583.26
; total filament weight [g] : 27.38,34.75
; model label id: 67,100
; filament_density: 1.26,1.26
; filament_diameter: 1.75,1.75
; max_z_height: 25.60
; HEADER_BLOCK_END

; CONFIG_BLOCK_START
; accel_to_decel_enable = 0
; accel_to_decel_factor = 50%
; activate_air_filtration = 0,0
; additional_cooling_fan_speed = 70,70
; auxiliary_fan = 0
; bed_custom_model = 
; bed_custom_texture = 
; bed_exclude_area = 
; before_layer_change_gcode = 
; best_object_pos = 0.5,0.5
; bottom_shell_layers = 3
; bottom_shell_thickness = 0
; bottom_surface_pattern = monotonic
; bridge_angle = 0
; bridge_flow = 1
; bridge_no_support = 0
; bridge_speed = 50
; brim_object_gap = 0.1
; brim_type = auto_brim
; brim_width = 5
; chamber_temperatures = 0,0
; change_filament_gcode = ;===== A1 20240913 =======================\nM1007 S0 ; turn off mass estimation\nG392 S0\nM620 S[next_extruder]A\nM204 S9000\n{if toolchange_count > 1}\nG17\nG2 Z{max_layer_z + 0.4} I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift\n{endif}\nG1 Z{max_layer_z + 3.0} F1200\n\nM400\nM106 P1 S0\nM106 P2 S0\n{if old_filament_temp > 142 && next_extruder < 255}\nM104 S[old_filament_temp]\n{endif}\n\nG1 X267 F18000\n\n{if long_retractions_when_cut[previous_extruder]}\nM620.11 S1 I[previous_extruder] E-{retraction_distances_when_cut[previous_extruder]} F1200\n{else}\nM620.11 S0\n{endif}\nM400\n\nM620.1 E F[old_filament_e_feedrate] T{nozzle_temperature_range_high[previous_extruder]}\nM620.10 A0 F[old_filament_e_feedrate]\nT[next_extruder]\nM620.1 E F[new_filament_e_feedrate] T{nozzle_temperature_range_high[next_extruder]}\nM620.10 A1 F[new_filament_e_feedrate] L[flush_length] H[nozzle_diameter] T[nozzle_temperature_range_high]\n\nG1 Y128 F9000\n\n{if next_extruder < 255}\n\n{if long_retractions_when_cut[previous_extruder]}\nM620.11 S1 I[previous_extruder] E{retraction_distances_when_cut[previous_extruder]} F{old_filament_e_feedrate}\nM628 S1\nG92 E0\nG1 E{retraction_distances_when_cut[previous_extruder]} F[old_filament_e_feedrate]\nM400\nM629 S1\n{else}\nM620.11 S0\n{endif}\n\nM400\nG92 E0\nM628 S0\n\n{if flush_length_1 > 1}\n; FLUSH_START\n; always use highest temperature to flush\nM400\nM1002 set_filament_type:UNKNOWN\nM109 S[nozzle_temperature_range_high]\nM106 P1 S60\n{if flush_length_1 > 23.7}\nG1 E23.7 F{old_filament_e_feedrate} ; do not need pulsatile flushing for start part\nG1 E{(flush_length_1 - 23.7) * 0.02} F50\nG1 E{(flush_length_1 - 23.7) * 0.23} F{old_filament_e_feedrate}\nG1 E{(flush_length_1 - 23.7) * 0.02} F50\nG1 E{(flush_length_1 - 23.7) * 0.23} F{new_filament_e_feedrate}\nG1 E{(flush_length_1 - 23.7) * 0.02} F50\nG1 E{(flush_length_1 - 23.7) * 0.23} F{new_filament_e_feedrate}\nG1 E{(flush_length_1 - 23.7) * 0.02} F50\nG1 E{(flush_length_1 - 23.7) * 0.23} F{new_filament_e_feedrate}\n{else}\nG1 E{flush_length_1} F{old_filament_e_feedrate}\n{endif}\n; FLUSH_END\nG1 E-[old_retract_length_toolchange] F1800\nG1 E[old_retract_length_toolchange] F300\nM400\nM1002 set_filament_type:{filament_type[next_extruder]}\n{endif}\n\n{if flush_length_1 > 45 && flush_length_2 > 1}\n; WIPE\nM400\nM106 P1 S178\nM400 S3\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nM400\nM106 P1 S0\n{endif}\n\n{if flush_length_2 > 1}\nM106 P1 S60\n; FLUSH_START\nG1 E{flush_length_2 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_2 * 0.02} F50\nG1 E{flush_length_2 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_2 * 0.02} F50\nG1 E{flush_length_2 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_2 * 0.02} F50\nG1 E{flush_length_2 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_2 * 0.02} F50\nG1 E{flush_length_2 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_2 * 0.02} F50\n; FLUSH_END\nG1 E-[new_retract_length_toolchange] F1800\nG1 E[new_retract_length_toolchange] F300\n{endif}\n\n{if flush_length_2 > 45 && flush_length_3 > 1}\n; WIPE\nM400\nM106 P1 S178\nM400 S3\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nM400\nM106 P1 S0\n{endif}\n\n{if flush_length_3 > 1}\nM106 P1 S60\n; FLUSH_START\nG1 E{flush_length_3 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_3 * 0.02} F50\nG1 E{flush_length_3 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_3 * 0.02} F50\nG1 E{flush_length_3 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_3 * 0.02} F50\nG1 E{flush_length_3 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_3 * 0.02} F50\nG1 E{flush_length_3 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_3 * 0.02} F50\n; FLUSH_END\nG1 E-[new_retract_length_toolchange] F1800\nG1 E[new_retract_length_toolchange] F300\n{endif}\n\n{if flush_length_3 > 45 && flush_length_4 > 1}\n; WIPE\nM400\nM106 P1 S178\nM400 S3\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nM400\nM106 P1 S0\n{endif}\n\n{if flush_length_4 > 1}\nM106 P1 S60\n; FLUSH_START\nG1 E{flush_length_4 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_4 * 0.02} F50\nG1 E{flush_length_4 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_4 * 0.02} F50\nG1 E{flush_length_4 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_4 * 0.02} F50\nG1 E{flush_length_4 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_4 * 0.02} F50\nG1 E{flush_length_4 * 0.18} F{new_filament_e_feedrate}\nG1 E{flush_length_4 * 0.02} F50\n; FLUSH_END\n{endif}\n\nM629\n\nM400\nM106 P1 S60\nM109 S[new_filament_temp]\nG1 E6 F{new_filament_e_feedrate} ;Compensate for filament spillage during waiting temperature\nM400\nG92 E0\nG1 E-[new_retract_length_toolchange] F1800\nM400\nM106 P1 S178\nM400 S3\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nG1 X-38.2 F18000\nG1 X-48.2 F3000\nM400\nG1 Z{max_layer_z + 3.0} F3000\nM106 P1 S0\n{if layer_z <= (initial_layer_print_height + 0.001)}\nM204 S[initial_layer_acceleration]\n{else}\nM204 S[default_acceleration]\n{endif}\n{else}\nG1 X[x_after_toolchange] Y[y_after_toolchange] Z[z_after_toolchange] F12000\n{endif}\n\nM622.1 S0\nM9833 F{outer_wall_volumetric_speed/2.4} A0.3 ; cali dynamic extrusion compensation\nM1002 judge_flag filament_need_cali_flag\nM622 J1\n  G92 E0\n  G1 E-[new_retract_length_toolchange] F1800\n  M400\n  \n  M106 P1 S178\n  M400 S4\n  G1 X-38.2 F18000\n  G1 X-48.2 F3000\n  G1 X-38.2 F18000 ;wipe and shake\n  G1 X-48.2 F3000\n  G1 X-38.2 F12000 ;wipe and shake\n  G1 X-48.2 F3000\n  M400\n  M106 P1 S0 \nM623\n\nM621 S[next_extruder]A\nG392 S0\n\nM1007 S1\n
; close_fan_the_first_x_layers = 1,1
; complete_print_exhaust_fan_speed = 70,70
; cool_plate_temp = 35,35
; cool_plate_temp_initial_layer = 35,35
; curr_bed_type = Textured PEI Plate
; default_acceleration = 6000
; default_filament_colour = ;
; default_filament_profile = "Bambu PLA Basic @BBL A1"
; default_jerk = 0
; default_print_profile = 0.20mm Standard @BBL A1
; deretraction_speed = 30
; detect_narrow_internal_solid_infill = 1
; detect_overhang_wall = 1
; detect_thin_wall = 0
; different_settings_to_system = post_process;;;
; draft_shield = disabled
; during_print_exhaust_fan_speed = 70,70
; elefant_foot_compensation = 0.075
; enable_arc_fitting = 1
; enable_long_retraction_when_cut = 2
; enable_overhang_bridge_fan = 1,1
; enable_overhang_speed = 1
; enable_pressure_advance = 0,0
; enable_prime_tower = 1
; enable_support = 0
; enforce_support_layers = 0
; eng_plate_temp = 0,0
; eng_plate_temp_initial_layer = 0,0
; ensure_vertical_shell_thickness = 1
; exclude_object = 1
; extruder_clearance_dist_to_rod = 56.5
; extruder_clearance_height_to_lid = 256
; extruder_clearance_height_to_rod = 25
; extruder_clearance_max_radius = 73
; extruder_colour = #018001
; extruder_offset = 0x0
; extruder_type = DirectDrive
; fan_cooling_layer_time = 80,80
; fan_max_speed = 80,80
; fan_min_speed = 60,60
; filament_colour = #00AE42;#FFFF00
; filament_cost = 24.99,24.99
; filament_density = 1.26,1.26
; filament_diameter = 1.75,1.75
; filament_end_gcode = "; filament end gcode \nM106 P3 S0\n";"; filament end gcode \nM106 P3 S0\n"
; filament_flow_ratio = 0.98,0.98
; filament_ids = GFA00;GFA00
; filament_is_support = 0,0
; filament_long_retractions_when_cut = 1,1
; filament_max_volumetric_speed = 21,21
; filament_minimal_purge_on_wipe_tower = 15,15
; filament_notes = 
; filament_retraction_distances_when_cut = 18,18
; filament_scarf_gap = 0%,0%
; filament_scarf_height = 10%,10%
; filament_scarf_length = 10,10
; filament_scarf_seam_type = none,none
; filament_settings_id = "Bambu PLA Basic @BBL A1";"Bambu PLA Basic @BBL A1"
; filament_shrink = 100%,100%
; filament_soluble = 0,0
; filament_start_gcode = "; filament start gcode\n{if  (bed_temperature[current_extruder] >55)||(bed_temperature_initial_layer[current_extruder] >55)}M106 P3 S200\n{elsif(bed_temperature[current_extruder] >50)||(bed_temperature_initial_layer[current_extruder] >50)}M106 P3 S150\n{elsif(bed_temperature[current_extruder] >45)||(bed_temperature_initial_layer[current_extruder] >45)}M106 P3 S50\n{endif}\n\n{if activate_air_filtration[current_extruder] && support_air_filtration}\nM106 P3 S{during_print_exhaust_fan_speed_num[current_extruder]} \n{endif}";"; filament start gcode\n{if  (bed_temperature[current_extruder] >55)||(bed_temperature_initial_layer[current_extruder] >55)}M106 P3 S200\n{elsif(bed_temperature[current_extruder] >50)||(bed_temperature_initial_layer[current_extruder] >50)}M106 P3 S150\n{elsif(bed_temperature[current_extruder] >45)||(bed_temperature_initial_layer[current_extruder] >45)}M106 P3 S50\n{endif}\n\n{if activate_air_filtration[current_extruder] && support_air_filtration}\nM106 P3 S{during_print_exhaust_fan_speed_num[current_extruder]} \n{endif}"
; filament_type = PLA;PLA
; filament_vendor = "Bambu Lab";"Bambu Lab"
; filename_format = {input_filename_base}_{filament_type[0]}_{print_time}.gcode
; filter_out_gap_fill = 0
; first_layer_print_sequence = 0
; flush_into_infill = 0
; flush_into_objects = 0
; flush_into_support = 1
; flush_multiplier = 1
; flush_volumes_matrix = 0,288,198,0
; flush_volumes_vector = 140,140,140,140
; full_fan_speed_layer = 0,0
; fuzzy_skin = none
; fuzzy_skin_point_distance = 0.8
; fuzzy_skin_thickness = 0.3
; gap_infill_speed = 250
; gcode_add_line_number = 0
; gcode_flavor = marlin
; has_scarf_joint_seam = 0
; head_wrap_detect_zone = 226x224,256x224,256x256,226x256
; host_type = octoprint
; hot_plate_temp = 65,65
; hot_plate_temp_initial_layer = 65,65
; independent_support_layer_height = 0
; infill_combination = 0
; infill_direction = 45
; infill_jerk = 9
; infill_wall_overlap = 15%
; initial_layer_acceleration = 500
; initial_layer_flow_ratio = 1
; initial_layer_infill_speed = 105
; initial_layer_jerk = 9
; initial_layer_line_width = 0.5
; initial_layer_print_height = 0.2
; initial_layer_speed = 50
; inner_wall_acceleration = 0
; inner_wall_jerk = 9
; inner_wall_line_width = 0.45
; inner_wall_speed = 300
; interface_shells = 0
; internal_bridge_support_thickness = 0.8
; internal_solid_infill_line_width = 0.42
; internal_solid_infill_pattern = zig-zag
; internal_solid_infill_speed = 250
; ironing_direction = 45
; ironing_flow = 10%
; ironing_inset = 0.21
; ironing_pattern = zig-zag
; ironing_spacing = 0.15
; ironing_speed = 30
; ironing_type = no ironing
; is_infill_first = 0
; layer_change_gcode = ; layer num/total_layer_count: {layer_num+1}/[total_layer_count]\n; update layer progress\nM73 L{layer_num+1}\nM991 S0 P{layer_num} ;notify layer change
; layer_height = 0.2
; line_width = 0.42
; long_retractions_when_cut = 0
; machine_end_gcode = ;===== date: 20231229 =====================\nG392 S0 ;turn off nozzle clog detect\n\nM400 ; wait for buffer to clear\nG92 E0 ; zero the extruder\nG1 E-0.8 F1800 ; retract\nG1 Z{max_layer_z + 0.5} F900 ; lower z a little\nG1 X0 Y{first_layer_center_no_wipe_tower[1]} F18000 ; move to safe pos\nG1 X-13.0 F3000 ; move to safe pos\n{if !spiral_mode && print_sequence != "by object"}\nM1002 judge_flag timelapse_record_flag\nM622 J1\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM400 P100\nM971 S11 C11 O0\nM991 S0 P-1 ;end timelapse at safe pos\nM623\n{endif}\n\nM140 S0 ; turn off bed\nM106 S0 ; turn off fan\nM106 P2 S0 ; turn off remote part cooling fan\nM106 P3 S0 ; turn off chamber cooling fan\n\n;G1 X27 F15000 ; wipe\n\n; pull back filament to AMS\nM620 S255\nG1 X267 F15000\nT255\nG1 X-28.5 F18000\nG1 X-48.2 F3000\nG1 X-28.5 F18000\nG1 X-48.2 F3000\nM621 S255\n\nM104 S0 ; turn off hotend\n\nM400 ; wait all motion done\nM17 S\nM17 Z0.4 ; lower z motor current to reduce impact if there is something in the bottom\n{if (max_layer_z + 100.0) < 256}\n    G1 Z{max_layer_z + 100.0} F600\n    G1 Z{max_layer_z +98.0}\n{else}\n    G1 Z256 F600\n    G1 Z256\n{endif}\nM400 P100\nM17 R ; restore z current\n\nG90\nG1 X-48 Y180 F3600\n\nM220 S100  ; Reset feedrate magnitude\nM201.2 K1.0 ; Reset acc magnitude\nM73.2   R1.0 ;Reset left time magnitude\nM1002 set_gcode_claim_speed_level : 0\n\n;=====printer finish  sound=========\nM17\nM400 S1\nM1006 S1\nM1006 A0 B20 L100 C37 D20 M40 E42 F20 N60\nM1006 A0 B10 L100 C44 D10 M60 E44 F10 N60\nM1006 A0 B10 L100 C46 D10 M80 E46 F10 N80\nM1006 A44 B20 L100 C39 D20 M60 E48 F20 N60\nM1006 A0 B10 L100 C44 D10 M60 E44 F10 N60\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N60\nM1006 A0 B10 L100 C39 D10 M60 E39 F10 N60\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N60\nM1006 A0 B10 L100 C44 D10 M60 E44 F10 N60\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N60\nM1006 A0 B10 L100 C39 D10 M60 E39 F10 N60\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N60\nM1006 A0 B10 L100 C48 D10 M60 E44 F10 N80\nM1006 A0 B10 L100 C0 D10 M60 E0 F10  N80\nM1006 A44 B20 L100 C49 D20 M80 E41 F20 N80\nM1006 A0 B20 L100 C0 D20 M60 E0 F20 N80\nM1006 A0 B20 L100 C37 D20 M30 E37 F20 N60\nM1006 W\n;=====printer finish  sound=========\n\n;M17 X0.8 Y0.8 Z0.5 ; lower motor current to 45% power\nM400\nM18 X Y Z\n\n
; machine_load_filament_time = 25
; machine_max_acceleration_e = 5000,5000
; machine_max_acceleration_extruding = 12000,12000
; machine_max_acceleration_retracting = 5000,5000
; machine_max_acceleration_travel = 9000,9000
; machine_max_acceleration_x = 12000,12000
; machine_max_acceleration_y = 12000,12000
; machine_max_acceleration_z = 1500,1500
; machine_max_jerk_e = 3,3
; machine_max_jerk_x = 9,9
; machine_max_jerk_y = 9,9
; machine_max_jerk_z = 3,3
; machine_max_speed_e = 30,30
; machine_max_speed_x = 500,200
; machine_max_speed_y = 500,200
; machine_max_speed_z = 30,30
; machine_min_extruding_rate = 0,0
; machine_min_travel_rate = 0,0
; machine_pause_gcode = M400 U1
; machine_start_gcode = ;===== machine: A1 =========================\n;===== date: 20240620 =====================\nG392 S0\nM9833.2\n;M400\n;M73 P1.717\n\n;===== start to heat heatbead&hotend==========\nM1002 gcode_claim_action : 2\nM1002 set_filament_type:{filament_type[initial_no_support_extruder]}\nM104 S140\nM140 S[bed_temperature_initial_layer_single]\n\n;=====start printer sound ===================\nM17\nM400 S1\nM1006 S1\nM1006 A0 B10 L100 C37 D10 M60 E37 F10 N60\nM1006 A0 B10 L100 C41 D10 M60 E41 F10 N60\nM1006 A0 B10 L100 C44 D10 M60 E44 F10 N60\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N60\nM1006 A43 B10 L100 C46 D10 M70 E39 F10 N80\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N80\nM1006 A0 B10 L100 C43 D10 M60 E39 F10 N80\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N80\nM1006 A0 B10 L100 C41 D10 M80 E41 F10 N80\nM1006 A0 B10 L100 C44 D10 M80 E44 F10 N80\nM1006 A0 B10 L100 C49 D10 M80 E49 F10 N80\nM1006 A0 B10 L100 C0 D10 M80 E0 F10 N80\nM1006 A44 B10 L100 C48 D10 M60 E39 F10 N80\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N80\nM1006 A0 B10 L100 C44 D10 M80 E39 F10 N80\nM1006 A0 B10 L100 C0 D10 M60 E0 F10 N80\nM1006 A43 B10 L100 C46 D10 M60 E39 F10 N80\nM1006 W\nM18 \n;=====start printer sound ===================\n\n;=====avoid end stop =================\nG91\nG380 S2 Z40 F1200\nG380 S3 Z-15 F1200\nG90\n\n;===== reset machine status =================\n;M290 X39 Y39 Z8\nM204 S6000\n\nM630 S0 P0\nG91\nM17 Z0.3 ; lower the z-motor current\n\nG90\nM17 X0.65 Y1.2 Z0.6 ; reset motor current to default\nM960 S5 P1 ; turn on logo lamp\nG90\nM220 S100 ;Reset Feedrate\nM221 S100 ;Reset Flowrate\nM73.2   R1.0 ;Reset left time magnitude\n;M211 X0 Y0 Z0 ; turn off soft endstop to prevent protential logic problem\n\n;====== cog noise reduction=================\nM982.2 S1 ; turn on cog noise reduction\n\nM1002 gcode_claim_action : 13\n\nG28 X\nG91\nG1 Z5 F1200\nG90\nG0 X128 F30000\nG0 Y254 F3000\nG91\nG1 Z-5 F1200\n\nM109 S25 H140\n\nM17 E0.3\nM83\nG1 E10 F1200\nG1 E-0.5 F30\nM17 D\n\nG28 Z P0 T140; home z with low precision,permit 300deg temperature\nM104 S{nozzle_temperature_initial_layer[initial_extruder]}\n\nM1002 judge_flag build_plate_detect_flag\nM622 S1\n  G39.4\n  G90\n  G1 Z5 F1200\nM623\n\n;M400\n;M73 P1.717\n\n;===== prepare print temperature and material ==========\nM1002 gcode_claim_action : 24\n\nM400\n;G392 S1\nM211 X0 Y0 Z0 ;turn off soft endstop\nM975 S1 ; turn on\n\nG90\nG1 X-28.5 F30000\nG1 X-48.2 F3000\n\nM620 M ;enable remap\nM620 S[initial_no_support_extruder]A   ; switch material if AMS exist\n    M1002 gcode_claim_action : 4\n    M400\n    M1002 set_filament_type:UNKNOWN\n    M109 S[nozzle_temperature_initial_layer]\n    M104 S250\n    M400\n    T[initial_no_support_extruder]\n    G1 X-48.2 F3000\n    M400\n\n    M620.1 E F{filament_max_volumetric_speed[initial_no_support_extruder]/2.4053*60} T{nozzle_temperature_range_high[initial_no_support_extruder]}\n    M109 S250 ;set nozzle to common flush temp\n    M106 P1 S0\n    G92 E0\n    G1 E50 F200\n    M400\n    M1002 set_filament_type:{filament_type[initial_no_support_extruder]}\nM621 S[initial_no_support_extruder]A\n\nM109 S{nozzle_temperature_range_high[initial_no_support_extruder]} H300\nG92 E0\nG1 E50 F200 ; lower extrusion speed to avoid clog\nM400\nM106 P1 S178\nG92 E0\nG1 E5 F200\nM104 S{nozzle_temperature_initial_layer[initial_no_support_extruder]}\nG92 E0\nG1 E-0.5 F300\n\nG1 X-28.5 F30000\nG1 X-48.2 F3000\nG1 X-28.5 F30000 ;wipe and shake\nG1 X-48.2 F3000\nG1 X-28.5 F30000 ;wipe and shake\nG1 X-48.2 F3000\n\n;G392 S0\n\nM400\nM106 P1 S0\n;===== prepare print temperature and material end =====\n\n;M400\n;M73 P1.717\n\n;===== auto extrude cali start =========================\nM975 S1\n;G392 S1\n\nG90\nM83\nT1000\nG1 X-48.2 Y0 Z10 F10000\nM400\nM1002 set_filament_type:UNKNOWN\n\nM412 S1 ;  ===turn on  filament runout detection===\nM400 P10\nM620.3 W1; === turn on filament tangle detection===\nM400 S2\n\nM1002 set_filament_type:{filament_type[initial_no_support_extruder]}\n\n;M1002 set_flag extrude_cali_flag=1\nM1002 judge_flag extrude_cali_flag\n\nM622 J1\n    M1002 gcode_claim_action : 8\n\n    M109 S{nozzle_temperature[initial_extruder]}\n    G1 E10 F{outer_wall_volumetric_speed/2.4*60}\n    M983 F{outer_wall_volumetric_speed/2.4} A0.3 H[nozzle_diameter]; cali dynamic extrusion compensation\n\n    M106 P1 S255\n    M400 S5\n    G1 X-28.5 F18000\n    G1 X-48.2 F3000\n    G1 X-28.5 F18000 ;wipe and shake\n    G1 X-48.2 F3000\n    G1 X-28.5 F12000 ;wipe and shake\n    G1 X-48.2 F3000\n    M400\n    M106 P1 S0\n\n    M1002 judge_last_extrude_cali_success\n    M622 J0\n        M983 F{outer_wall_volumetric_speed/2.4} A0.3 H[nozzle_diameter]; cali dynamic extrusion compensation\n        M106 P1 S255\n        M400 S5\n        G1 X-28.5 F18000\n        G1 X-48.2 F3000\n        G1 X-28.5 F18000 ;wipe and shake\n        G1 X-48.2 F3000\n        G1 X-28.5 F12000 ;wipe and shake\n        M400\n        M106 P1 S0\n    M623\n    \n    G1 X-48.2 F3000\n    M400\n    M984 A0.1 E1 S1 F{outer_wall_volumetric_speed/2.4} H[nozzle_diameter]\n    M106 P1 S178\n    M400 S7\n    G1 X-28.5 F18000\n    G1 X-48.2 F3000\n    G1 X-28.5 F18000 ;wipe and shake\n    G1 X-48.2 F3000\n    G1 X-28.5 F12000 ;wipe and shake\n    G1 X-48.2 F3000\n    M400\n    M106 P1 S0\nM623 ; end of "draw extrinsic para cali paint"\n\n;G392 S0\n;===== auto extrude cali end ========================\n\n;M400\n;M73 P1.717\n\nM104 S170 ; prepare to wipe nozzle\nM106 S255 ; turn on fan\n\n;===== mech mode fast check start =====================\nM1002 gcode_claim_action : 3\n\nG1 X128 Y128 F20000\nG1 Z5 F1200\nM400 P200\nM970.3 Q1 A5 K0 O3\nM974 Q1 S2 P0\n\nM970.2 Q1 K1 W58 Z0.1\nM974 S2\n\nG1 X128 Y128 F20000\nG1 Z5 F1200\nM400 P200\nM970.3 Q0 A10 K0 O1\nM974 Q0 S2 P0\n\nM970.2 Q0 K1 W78 Z0.1\nM974 S2\n\nM975 S1\nG1 F30000\nG1 X0 Y5\nG28 X ; re-home XY\n\nG1 Z4 F1200\n\n;===== mech mode fast check end =======================\n\n;M400\n;M73 P1.717\n\n;===== wipe nozzle ===============================\nM1002 gcode_claim_action : 14\n\nM975 S1\nM106 S255 ; turn on fan (G28 has turn off fan)\nM211 S; push soft endstop status\nM211 X0 Y0 Z0 ;turn off Z axis endstop\n\n;===== remove waste by touching start =====\n\nM104 S170 ; set temp down to heatbed acceptable\n\nM83\nG1 E-1 F500\nG90\nM83\n\nM109 S170\nG0 X108 Y-0.5 F30000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X110 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X112 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X114 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X116 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X118 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X120 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X122 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X124 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X126 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X128 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X130 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X132 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X134 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X136 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X138 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X140 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X142 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X144 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X146 F10000\nG380 S3 Z-5 F1200\nG1 Z2 F1200\nG1 X148 F10000\nG380 S3 Z-5 F1200\n\nG1 Z5 F30000\n;===== remove waste by touching end =====\n\nG1 Z10 F1200\nG0 X118 Y261 F30000\nG1 Z5 F1200\nM109 S{nozzle_temperature_initial_layer[initial_extruder]-50}\n\nG28 Z P0 T300; home z with low precision,permit 300deg temperature\nG29.2 S0 ; turn off ABL\nM104 S140 ; prepare to abl\nG0 Z5 F20000\n\nG0 X128 Y261 F20000  ; move to exposed steel surface\nG0 Z-1.01 F1200      ; stop the nozzle\n\nG91\nG2 I1 J0 X2 Y0 F2000.1\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\n\nG90\nG1 Z10 F1200\n\n;===== brush material wipe nozzle =====\n\nG90\nG1 Y250 F30000\nG1 X55\nG1 Z1.300 F1200\nG1 Y262.5 F6000\nG91\nG1 X-35 F30000\nG1 Y-0.5\nG1 X45\nG1 Y-0.5\nG1 X-45\nG1 Y-0.5\nG1 X45\nG1 Y-0.5\nG1 X-45\nG1 Y-0.5\nG1 X45\nG1 Z5.000 F1200\n\nG90\nG1 X30 Y250.000 F30000\nG1 Z1.300 F1200\nG1 Y262.5 F6000\nG91\nG1 X35 F30000\nG1 Y-0.5\nG1 X-45\nG1 Y-0.5\nG1 X45\nG1 Y-0.5\nG1 X-45\nG1 Y-0.5\nG1 X45\nG1 Y-0.5\nG1 X-45\nG1 Z10.000 F1200\n\n;===== brush material wipe nozzle end =====\n\nG90\n;G0 X128 Y261 F20000  ; move to exposed steel surface\nG1 Y250 F30000\nG1 X138\nG1 Y261\nG0 Z-1.01 F1200      ; stop the nozzle\n\nG91\nG2 I1 J0 X2 Y0 F2000.1\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\nG2 I1 J0 X2\nG2 I-0.75 J0 X-1.5\n\nM109 S140\nM106 S255 ; turn on fan (G28 has turn off fan)\n\nM211 R; pop softend status\n\n;===== wipe nozzle end ================================\n\n;M400\n;M73 P1.717\n\n;===== bed leveling ==================================\nM1002 judge_flag g29_before_print_flag\n\nG90\nG1 Z5 F1200\nG1 X0 Y0 F30000\nG29.2 S1 ; turn on ABL\n\nM190 S[bed_temperature_initial_layer_single]; ensure bed temp\nM109 S140\nM106 S0 ; turn off fan , too noisy\n\nM622 J1\n    M1002 gcode_claim_action : 1\n    G29 A1 X{first_layer_print_min[0]} Y{first_layer_print_min[1]} I{first_layer_print_size[0]} J{first_layer_print_size[1]}\n    M400\n    M500 ; save cali data\nM623\n;===== bed leveling end ================================\n\n;===== home after wipe mouth============================\nM1002 judge_flag g29_before_print_flag\nM622 J0\n\n    M1002 gcode_claim_action : 13\n    G28\n\nM623\n\n;===== home after wipe mouth end =======================\n\n;M400\n;M73 P1.717\n\nG1 X108.000 Y-0.500 F30000\nG1 Z0.300 F1200\nM400\nG2814 Z0.32\n\nM104 S{nozzle_temperature_initial_layer[initial_extruder]} ; prepare to print\n\n;===== nozzle load line ===============================\n;G90\n;M83\n;G1 Z5 F1200\n;G1 X88 Y-0.5 F20000\n;G1 Z0.3 F1200\n\n;M109 S{nozzle_temperature_initial_layer[initial_extruder]}\n\n;G1 E2 F300\n;G1 X168 E4.989 F6000\n;G1 Z1 F1200\n;===== nozzle load line end ===========================\n\n;===== extrude cali test ===============================\n\nM400\n    M900 S\n    M900 C\n    G90\n    M83\n\n    M109 S{nozzle_temperature_initial_layer[initial_extruder]}\n    G0 X128 E8  F{outer_wall_volumetric_speed/(24/20)    * 60}\n    G0 X133 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)/4     * 60}\n    G0 X138 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)     * 60}\n    G0 X143 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)/4     * 60}\n    G0 X148 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)     * 60}\n    G0 X153 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)/4     * 60}\n    G91\n    G1 X1 Z-0.300\n    G1 X4\n    G1 Z1 F1200\n    G90\n    M400\n\nM900 R\n\nM1002 judge_flag extrude_cali_flag\nM622 J1\n    G90\n    G1 X108.000 Y1.000 F30000\n    G91\n    G1 Z-0.700 F1200\n    G90\n    M83\n    G0 X128 E10  F{outer_wall_volumetric_speed/(24/20)    * 60}\n    G0 X133 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)/4     * 60}\n    G0 X138 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)     * 60}\n    G0 X143 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)/4     * 60}\n    G0 X148 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)     * 60}\n    G0 X153 E.3742  F{outer_wall_volumetric_speed/(0.3*0.5)/4     * 60}\n    G91\n    G1 X1 Z-0.300\n    G1 X4\n    G1 Z1 F1200\n    G90\n    M400\nM623\n\nG1 Z0.2\n\n;M400\n;M73 P1.717\n\n;========turn off light and wait extrude temperature =============\nM1002 gcode_claim_action : 0\nM400\n\n;===== for Textured PEI Plate , lower the nozzle as the nozzle was touching topmost of the texture when homing ==\n;curr_bed_type={curr_bed_type}\n{if curr_bed_type=="Textured PEI Plate"}\nG29.1 Z{-0.02} ; for Textured PEI Plate\n{endif}\n\nM960 S1 P0 ; turn off laser\nM960 S2 P0 ; turn off laser\nM106 S0 ; turn off fan\nM106 P2 S0 ; turn off big fan\nM106 P3 S0 ; turn off chamber fan\n\nM975 S1 ; turn on mech mode supression\nG90\nM83\nT1000\n\nM211 X0 Y0 Z0 ;turn off soft endstop\n;G392 S1 ; turn on clog detection\nM1007 S1 ; turn on mass estimation\nG29.4\n
; machine_unload_filament_time = 29
; max_bridge_length = 0
; max_layer_height = 0.28
; max_travel_detour_distance = 0
; min_bead_width = 85%
; min_feature_size = 25%
; min_layer_height = 0.08
; minimum_sparse_infill_area = 15
; mmu_segmented_region_interlocking_depth = 0
; mmu_segmented_region_max_width = 0
; nozzle_diameter = 0.4
; nozzle_height = 4.76
; nozzle_temperature = 220,220
; nozzle_temperature_initial_layer = 220,220
; nozzle_temperature_range_high = 240,240
; nozzle_temperature_range_low = 190,190
; nozzle_type = stainless_steel
; nozzle_volume = 92
; only_one_wall_first_layer = 0
; ooze_prevention = 0
; other_layers_print_sequence = 0
; other_layers_print_sequence_nums = 0
; outer_wall_acceleration = 5000
; outer_wall_jerk = 9
; outer_wall_line_width = 0.42
; outer_wall_speed = 200
; overhang_1_4_speed = 0
; overhang_2_4_speed = 50
; overhang_3_4_speed = 30
; overhang_4_4_speed = 10
; overhang_fan_speed = 100,100
; overhang_fan_threshold = 50%,50%
; overhang_threshold_participating_cooling = 95%,95%
; overhang_totally_speed = 19
; post_process = "\"C:\\Users\\cshHo\\Development\\Slic3rPostProcessingUploader\\Slic3rPostProcessingUploaderUnitTests\\bin\\Debug\\net8.0\\Slic3rPostProcessingUploader.exe\" --debug \"C:\\tmp\\bambu\" --local-dev"
; precise_z_height = 0
; pressure_advance = 0.02,0.02
; prime_tower_brim_width = 3
; prime_tower_width = 35
; prime_volume = 45
; print_compatible_printers = "Bambu Lab A1 0.4 nozzle"
; print_flow_ratio = 1
; print_sequence = by layer
; print_settings_id = 0.20mm Standard @BBL A1
; printable_area = 0x0,256x0,256x256,0x256
; printable_height = 256
; printer_model = Bambu Lab A1
; printer_notes = 
; printer_settings_id = Bambu Lab A1 0.4 nozzle
; printer_structure = i3
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
; reduce_fan_stop_start_freq = 1,1
; reduce_infill_retraction = 1
; required_nozzle_HRC = 3,3
; resolution = 0.012
; retract_before_wipe = 0%
; retract_length_toolchange = 2
; retract_lift_above = 0
; retract_lift_below = 255
; retract_restart_extra = 0
; retract_restart_extra_toolchange = 0
; retract_when_changing_layer = 1
; retraction_distances_when_cut = 18
; retraction_length = 0.8
; retraction_minimum_travel = 1
; retraction_speed = 30
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
; single_extruder_multi_material = 1
; skirt_distance = 2
; skirt_height = 1
; skirt_loops = 0
; slice_closing_radius = 0.049
; slicing_mode = regular
; slow_down_for_layer_cooling = 1,1
; slow_down_layer_time = 6,6
; slow_down_min_speed = 20,20
; small_perimeter_speed = 50%
; small_perimeter_threshold = 0
; smooth_coefficient = 80
; smooth_speed_discontinuity_area = 1
; solid_infill_filament = 1
; sparse_infill_acceleration = 100%
; sparse_infill_anchor = 400%
; sparse_infill_anchor_max = 20
; sparse_infill_density = 15%
; sparse_infill_filament = 1
; sparse_infill_line_width = 0.45
; sparse_infill_pattern = grid
; sparse_infill_speed = 270
; spiral_mode = 0
; spiral_mode_max_xy_smoothing = 200%
; spiral_mode_smooth = 0
; standby_temperature_delta = -5
; start_end_points = 30x-3,54x245
; supertack_plate_temp = 45,45
; supertack_plate_temp_initial_layer = 45,45
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
; support_interface_bottom_layers = 2
; support_interface_filament = 0
; support_interface_loop_pattern = 0
; support_interface_not_for_body = 1
; support_interface_pattern = auto
; support_interface_spacing = 0.5
; support_interface_speed = 80
; support_interface_top_layers = 2
; support_line_width = 0.42
; support_object_first_layer_gap = 0.2
; support_object_xy_distance = 0.35
; support_on_build_plate_only = 0
; support_remove_small_overhang = 1
; support_speed = 150
; support_style = default
; support_threshold_angle = 30
; support_top_z_distance = 0.2
; support_type = normal(auto)
; temperature_vitrification = 45,45
; template_custom_gcode = 
; textured_plate_temp = 65,65
; textured_plate_temp_initial_layer = 65,65
; thick_bridges = 0
; thumbnail_size = 50x50
; time_lapse_gcode = ;===================== date: 20240606 =====================\n{if !spiral_mode && print_sequence != "by object"}\n; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer\nM622.1 S1 ; for prev firware, default turned on\nM1002 judge_flag timelapse_record_flag\nM622 J1\nG92 E0\nG17\nG2 Z{layer_z + 0.4} I0.86 J0.86 P1 F20000 ; spiral lift a little\nG1 Z{max_layer_z + 0.4}\nG1 X0 Y{first_layer_center_no_wipe_tower[1]} F18000 ; move to safe pos\nG1 X-48.2 F3000 ; move to safe pos\nM400 P300\nM971 S11 C11 O0\nG92 E0\nG1 X0 F18000\nM623\n\nM622.1 S1\nM1002 judge_flag g39_3rd_layer_detect_flag\nM622 J1\n    ; enable nozzle clog detect at 3rd layer\n    {if layer_num == 2}\n      M400\n      G90\n      M83\n      M204 S5000\n      G0 Z2 F4000\n      G0 X261 Y250 F20000\n      M400 P200\n      G39 S1\n      G0 Z2 F4000\n    {endif}\n\n\n    M622.1 S1\n    M1002 judge_flag g39_detection_flag\n    M622 J1\n      {if !in_head_wrap_detect_zone}\n        M622.1 S0\n        M1002 judge_flag g39_mass_exceed_flag\n        M622 J1\n        {if layer_num > 2}\n            G392 S0\n            M400\n            G90\n            M83\n            M204 S5000\n            G0 Z{max_layer_z + 0.4} F4000\n            G39.3 S1\n            G0 Z{max_layer_z + 0.4} F4000\n            G392 S0\n          {endif}\n        M623\n    {endif}\n    M623\nM623\n{endif}\n
; timelapse_type = 0
; top_area_threshold = 100%
; top_one_wall_type = all top
; top_shell_layers = 5
; top_shell_thickness = 1
; top_solid_infill_flow_ratio = 1
; top_surface_acceleration = 2000
; top_surface_jerk = 9
; top_surface_line_width = 0.42
; top_surface_pattern = monotonicline
; top_surface_speed = 200
; travel_jerk = 9
; travel_speed = 700
; travel_speed_z = 0
; tree_support_branch_angle = 45
; tree_support_branch_diameter = 2
; tree_support_branch_diameter_angle = 5
; tree_support_branch_distance = 5
; tree_support_wall_count = 0
; upward_compatible_machine = 
; use_firmware_retraction = 0
; use_relative_e_distances = 1
; wall_distribution_count = 1
; wall_filament = 1
; wall_generator = classic
; wall_loops = 2
; wall_sequence = inner wall/outer wall
; wall_transition_angle = 10
; wall_transition_filter_deviation = 25%
; wall_transition_length = 100%
; wipe = 1
; wipe_distance = 2
; wipe_speed = 80%
; wipe_tower_no_sparse_layers = 0
; wipe_tower_rotation_angle = 0
; wipe_tower_x = 15
; wipe_tower_y = 221
; xy_contour_compensation = 0
; xy_hole_compensation = 0
; z_hop = 0.4
; z_hop_types = Auto Lift
; CONFIG_BLOCK_END

; EXECUTABLE_BLOCK_START
M73 P0 R252
M201 X12000 Y12000 Z1500 E5000
M203 X500 Y500 Z30 E30
M204 P12000 R5000 T12000
M205 X9.00 Y9.00 Z3.00 E3.00
M106 S0
M106 P2 S0
; FEATURE: Custom
;===== machine: A1 =========================
;===== date: 20240620 =====================
G392 S0
M9833.2
;M400
;M73 P1.717

;===== start to heat heatbead&hotend==========
M1002 gcode_claim_action : 2
M1002 set_filament_type:PLA
M104 S140
M140 S65

;=====start printer sound ===================
M17
M400 S1
M1006 S1
M1006 A0 B10 L100 C37 D10 M60 E37 F10 N60
M1006 A0 B10 L100 C41 D10 M60 E41 F10 N60
M1006 A0 B10 L100 C44 D10 M60 E44 F10 N60
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N60
M1006 A43 B10 L100 C46 D10 M70 E39 F10 N80
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N80
M1006 A0 B10 L100 C43 D10 M60 E39 F10 N80
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N80
M1006 A0 B10 L100 C41 D10 M80 E41 F10 N80
M1006 A0 B10 L100 C44 D10 M80 E44 F10 N80
M1006 A0 B10 L100 C49 D10 M80 E49 F10 N80
M1006 A0 B10 L100 C0 D10 M80 E0 F10 N80
M1006 A44 B10 L100 C48 D10 M60 E39 F10 N80
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N80
M1006 A0 B10 L100 C44 D10 M80 E39 F10 N80
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N80
M1006 A43 B10 L100 C46 D10 M60 E39 F10 N80
M1006 W
M18 
;=====start printer sound ===================

;=====avoid end stop =================
G91
G380 S2 Z40 F1200
G380 S3 Z-15 F1200
G90

;===== reset machine status =================
;M290 X39 Y39 Z8
M204 S6000

M630 S0 P0
G91
M17 Z0.3 ; lower the z-motor current

G90
M17 X0.65 Y1.2 Z0.6 ; reset motor current to default
M960 S5 P1 ; turn on logo lamp
G90
M220 S100 ;Reset Feedrate
M221 S100 ;Reset Flowrate
M73.2   R1.0 ;Reset left time magnitude
;M211 X0 Y0 Z0 ; turn off soft endstop to prevent protential logic problem

;====== cog noise reduction=================
M982.2 S1 ; turn on cog noise reduction

M1002 gcode_claim_action : 13

G28 X
G91
G1 Z5 F1200
G90
G0 X128 F30000
G0 Y254 F3000
G91
G1 Z-5 F1200

M109 S25 H140

M17 E0.3
M83
G1 E10 F1200
G1 E-0.5 F30
M17 D

G28 Z P0 T140; home z with low precision,permit 300deg temperature
M104 S220

M1002 judge_flag build_plate_detect_flag
M622 S1
  G39.4
  G90
  G1 Z5 F1200
M623

;M400
;M73 P1.717

;===== prepare print temperature and material ==========
M1002 gcode_claim_action : 24

M400
;G392 S1
M211 X0 Y0 Z0 ;turn off soft endstop
M975 S1 ; turn on

G90
G1 X-28.5 F30000
G1 X-48.2 F3000

M620 M ;enable remap
M620 S0A   ; switch material if AMS exist
    M1002 gcode_claim_action : 4
    M400
    M1002 set_filament_type:UNKNOWN
    M109 S220
    M104 S250
    M400
    T0
    G1 X-48.2 F3000
    M400

    M620.1 E F523.843 T240
    M109 S250 ;set nozzle to common flush temp
    M106 P1 S0
    G92 E0
    G1 E50 F200
    M400
    M1002 set_filament_type:PLA
M621 S0A

M109 S240 H300
G92 E0
G1 E50 F200 ; lower extrusion speed to avoid clog
M400
M106 P1 S178
G92 E0
G1 E5 F200
M104 S220
G92 E0
G1 E-0.5 F300

G1 X-28.5 F30000
G1 X-48.2 F3000
G1 X-28.5 F30000 ;wipe and shake
G1 X-48.2 F3000
G1 X-28.5 F30000 ;wipe and shake
G1 X-48.2 F3000

;G392 S0

M400
M106 P1 S0
;===== prepare print temperature and material end =====

;M400
;M73 P1.717

;===== auto extrude cali start =========================
M975 S1
;G392 S1

G90
M83
T1000
G1 X-48.2 Y0 Z10 F10000
M400
M1002 set_filament_type:UNKNOWN

M412 S1 ;  ===turn on  filament runout detection===
M400 P10
M620.3 W1; === turn on filament tangle detection===
M400 S2

M1002 set_filament_type:PLA

;M1002 set_flag extrude_cali_flag=1
M1002 judge_flag extrude_cali_flag

M622 J1
    M1002 gcode_claim_action : 8

    M109 S220
    G1 E10 F377.08
    M983 F6.28466 A0.3 H0.4; cali dynamic extrusion compensation

    M106 P1 S255
    M400 S5
    G1 X-28.5 F18000
    G1 X-48.2 F3000
    G1 X-28.5 F18000 ;wipe and shake
    G1 X-48.2 F3000
    G1 X-28.5 F12000 ;wipe and shake
    G1 X-48.2 F3000
    M400
    M106 P1 S0

    M1002 judge_last_extrude_cali_success
    M622 J0
        M983 F6.28466 A0.3 H0.4; cali dynamic extrusion compensation
        M106 P1 S255
        M400 S5
        G1 X-28.5 F18000
        G1 X-48.2 F3000
        G1 X-28.5 F18000 ;wipe and shake
        G1 X-48.2 F3000
        G1 X-28.5 F12000 ;wipe and shake
        M400
        M106 P1 S0
    M623

M73 P0 R251
    G1 X-48.2 F3000
    M400
    M984 A0.1 E1 S1 F6.28466 H0.4
    M106 P1 S178
    M400 S7
    G1 X-28.5 F18000
    G1 X-48.2 F3000
    G1 X-28.5 F18000 ;wipe and shake
    G1 X-48.2 F3000
    G1 X-28.5 F12000 ;wipe and shake
    G1 X-48.2 F3000
    M400
    M106 P1 S0
M623 ; end of "draw extrinsic para cali paint"

;G392 S0
;===== auto extrude cali end ========================

;M400
;M73 P1.717

M104 S170 ; prepare to wipe nozzle
M106 S255 ; turn on fan

;===== mech mode fast check start =====================
M1002 gcode_claim_action : 3

G1 X128 Y128 F20000
G1 Z5 F1200
M400 P200
M970.3 Q1 A5 K0 O3
M974 Q1 S2 P0

M970.2 Q1 K1 W58 Z0.1
M974 S2

G1 X128 Y128 F20000
G1 Z5 F1200
M400 P200
M970.3 Q0 A10 K0 O1
M974 Q0 S2 P0

M970.2 Q0 K1 W78 Z0.1
M974 S2

M975 S1
G1 F30000
G1 X0 Y5
G28 X ; re-home XY

G1 Z4 F1200

;===== mech mode fast check end =======================

;M400
;M73 P1.717

;===== wipe nozzle ===============================
M1002 gcode_claim_action : 14

M975 S1
M106 S255 ; turn on fan (G28 has turn off fan)
M211 S; push soft endstop status
M211 X0 Y0 Z0 ;turn off Z axis endstop

;===== remove waste by touching start =====

M104 S170 ; set temp down to heatbed acceptable

M83
G1 E-1 F500
G90
M83

M109 S170
G0 X108 Y-0.5 F30000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X110 F10000
G380 S3 Z-5 F1200
M73 P2 R247
G1 Z2 F1200
G1 X112 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X114 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X116 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X118 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X120 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X122 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X124 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X126 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X128 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X130 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X132 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X134 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X136 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X138 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X140 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X142 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X144 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X146 F10000
G380 S3 Z-5 F1200
G1 Z2 F1200
G1 X148 F10000
G380 S3 Z-5 F1200

G1 Z5 F30000
;===== remove waste by touching end =====

G1 Z10 F1200
G0 X118 Y261 F30000
G1 Z5 F1200
M109 S170

G28 Z P0 T300; home z with low precision,permit 300deg temperature
G29.2 S0 ; turn off ABL
M104 S140 ; prepare to abl
G0 Z5 F20000

G0 X128 Y261 F20000  ; move to exposed steel surface
G0 Z-1.01 F1200      ; stop the nozzle

G91
G2 I1 J0 X2 Y0 F2000.1
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5

G90
G1 Z10 F1200

;===== brush material wipe nozzle =====

G90
G1 Y250 F30000
G1 X55
G1 Z1.300 F1200
G1 Y262.5 F6000
G91
G1 X-35 F30000
G1 Y-0.5
G1 X45
G1 Y-0.5
G1 X-45
G1 Y-0.5
G1 X45
G1 Y-0.5
G1 X-45
G1 Y-0.5
G1 X45
G1 Z5.000 F1200

G90
G1 X30 Y250.000 F30000
G1 Z1.300 F1200
G1 Y262.5 F6000
G91
G1 X35 F30000
G1 Y-0.5
G1 X-45
G1 Y-0.5
G1 X45
G1 Y-0.5
G1 X-45
G1 Y-0.5
G1 X45
G1 Y-0.5
G1 X-45
G1 Z10.000 F1200

;===== brush material wipe nozzle end =====

G90
;G0 X128 Y261 F20000  ; move to exposed steel surface
G1 Y250 F30000
G1 X138
G1 Y261
G0 Z-1.01 F1200      ; stop the nozzle

G91
G2 I1 J0 X2 Y0 F2000.1
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5
G2 I1 J0 X2
G2 I-0.75 J0 X-1.5

M109 S140
M106 S255 ; turn on fan (G28 has turn off fan)

M211 R; pop softend status

;===== wipe nozzle end ================================

;M400
;M73 P1.717

;===== bed leveling ==================================
M1002 judge_flag g29_before_print_flag

G90
G1 Z5 F1200
G1 X0 Y0 F30000
G29.2 S1 ; turn on ABL

M190 S65; ensure bed temp
M109 S140
M106 S0 ; turn off fan , too noisy

M622 J1
    M1002 gcode_claim_action : 1
    G29 A1 X11.5719 Y117.559 I158.267 J127.369
    M400
    M500 ; save cali data
M623
;===== bed leveling end ================================

;===== home after wipe mouth============================
M1002 judge_flag g29_before_print_flag
M622 J0

    M1002 gcode_claim_action : 13
    G28

M623

;===== home after wipe mouth end =======================

;M400
;M73 P1.717

G1 X108.000 Y-0.500 F30000
G1 Z0.300 F1200
M400
G2814 Z0.32

M104 S220 ; prepare to print

;===== nozzle load line ===============================
;G90
;M83
;G1 Z5 F1200
;G1 X88 Y-0.5 F20000
;G1 Z0.3 F1200

;M109 S220

;G1 E2 F300
;G1 X168 E4.989 F6000
;G1 Z1 F1200
;===== nozzle load line end ===========================

;===== extrude cali test ===============================

M400
    M900 S
    M900 C
    G90
    M83

    M109 S220
    G0 X128 E8  F904.991
    G0 X133 E.3742  F1508.32
    G0 X138 E.3742  F6033.27
    G0 X143 E.3742  F1508.32
    G0 X148 E.3742  F6033.27
    G0 X153 E.3742  F1508.32
    G91
    G1 X1 Z-0.300
    G1 X4
    G1 Z1 F1200
    G90
    M400

M900 R

M1002 judge_flag extrude_cali_flag
M622 J1
    G90
    G1 X108.000 Y1.000 F30000
    G91
    G1 Z-0.700 F1200
    G90
    M83
    G0 X128 E10  F904.991
    G0 X133 E.3742  F1508.32
    G0 X138 E.3742  F6033.27
    G0 X143 E.3742  F1508.32
    G0 X148 E.3742  F6033.27
    G0 X153 E.3742  F1508.32
    G91
    G1 X1 Z-0.300
    G1 X4
    G1 Z1 F1200
    G90
    M400
M623

G1 Z0.2

;M400
;M73 P1.717

;========turn off light and wait extrude temperature =============
M1002 gcode_claim_action : 0
M400

;===== for Textured PEI Plate , lower the nozzle as the nozzle was touching topmost of the texture when homing ==
;curr_bed_type=Textured PEI Plate

G29.1 Z-0.02 ; for Textured PEI Plate


M960 S1 P0 ; turn off laser
M960 S2 P0 ; turn off laser
M106 S0 ; turn off fan
M106 P2 S0 ; turn off big fan
M106 P3 S0 ; turn off chamber fan

M975 S1 ; turn on mech mode supression
G90
M83
T1000

M211 X0 Y0 Z0 ;turn off soft endstop
;G392 S1 ; turn on clog detection
M1007 S1 ; turn on mass estimation
G29.4
G90
G21
M83 ; use relative distances for extrusion
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S0A
M204 S9000

G1 Z3 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I0 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T0
M620.1 E F523 T240
M620.10 A1 F523 L0 H0.4 T240

G1 Y128 F9000




M620.11 S1 I0 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0















M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z3 F3000
M106 P1 S0

M204 S500



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S0A
G392 S0

M1007 S1
;_FORCE_RESUME_FAN_SPEED
; filament start gcode
M106 P3 S200


M981 S1 P20000 ;open spaghetti detector
G1 Z.2 F42000
; CHANGE_LAYER
; Z_HEIGHT: 0.2
; LAYER_HEIGHT: 0.2
; layer num/total_layer_count: 1/128
; update layer progress
M73 L1
M991 S0 P0 ;notify layer change
M106 S0
M106 P2 S0
M204 S500
G1 E-.8 F1800
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G1 X138.048 Y142.732 F42000
G1 Z.6
G1 Z.2
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.5
; LAYER_HEIGHT: 0.2
G1 F3000
G1 X114.012 Y142.732 E.89524
G1 X114.012 Y118.696 E.89524
G1 X138.048 Y118.696 E.89524
G1 X138.048 Y142.672 E.89301
M73 P2 R246
G1 X138.505 Y143.189 F42000
; FEATURE: Outer wall
G1 F3000
G1 X113.555 Y143.189 E.92929
G1 X113.555 Y118.239 E.92929
G1 X138.505 Y118.239 E.92929
G1 X138.505 Y143.129 E.92706
; WIPE_START
G1 X136.505 Y143.134 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X136.641 Y135.503 Z.6 F42000
G1 X136.936 Y118.879 Z.6
G1 Z.2
G1 E.8 F1800
; FEATURE: Bottom surface
; LINE_WIDTH: 0.502362
G1 F6300
G1 X137.659 Y119.602 E.0383
G1 X137.659 Y120.252 E.02433
G1 X136.492 Y119.085 E.06181
G1 X135.842 Y119.085 E.02433
G1 X137.659 Y120.902 E.09621
G1 X137.659 Y121.552 E.02433
G1 X135.193 Y119.085 E.13061
G1 X134.543 Y119.085 E.02433
G1 X137.659 Y122.201 E.16501
G1 X137.659 Y122.851 E.02433
G1 X133.893 Y119.085 E.19941
G1 X133.243 Y119.085 E.02433
G1 X137.659 Y123.501 E.23382
G1 X137.659 Y124.151 E.02433
G1 X132.594 Y119.085 E.26822
G1 X131.944 Y119.085 E.02433
G1 X137.659 Y124.8 E.30262
G1 X137.659 Y125.45 E.02433
G1 X131.294 Y119.085 E.33702
G1 X130.644 Y119.085 E.02433
G1 X137.659 Y126.1 E.37142
G1 X137.659 Y126.75 E.02433
G1 X129.995 Y119.085 E.40583
G1 X129.345 Y119.085 E.02433
G1 X137.659 Y127.399 E.44023
G1 X137.659 Y128.049 E.02433
G1 X128.695 Y119.085 E.47463
G1 X128.045 Y119.085 E.02433
G1 X137.659 Y128.699 E.50903
G1 X137.659 Y129.349 E.02433
G1 X127.396 Y119.085 E.54343
G1 X126.746 Y119.085 E.02433
G1 X137.659 Y129.998 E.57784
G1 X137.659 Y130.648 E.02433
G1 X126.096 Y119.085 E.61224
G1 X125.446 Y119.085 E.02433
G1 X137.659 Y131.298 E.64664
G1 X137.659 Y131.948 E.02433
G1 X124.797 Y119.085 E.68104
G1 X124.147 Y119.085 E.02433
G1 X137.659 Y132.597 E.71544
G1 X137.659 Y133.247 E.02433
G1 X123.497 Y119.085 E.74984
G1 X122.847 Y119.085 E.02433
G1 X137.659 Y133.897 E.78425
G1 X137.659 Y134.547 E.02433
G1 X122.198 Y119.085 E.81865
G1 X121.548 Y119.085 E.02433
G1 X137.659 Y135.196 E.85305
G1 X137.659 Y135.846 E.02433
G1 X120.898 Y119.085 E.88745
G1 X120.248 Y119.085 E.02433
G1 X137.659 Y136.496 E.92185
G1 X137.659 Y137.146 E.02433
G1 X119.599 Y119.085 E.95626
G1 X118.949 Y119.085 E.02433
G1 X137.659 Y137.795 E.99066
G1 X137.659 Y138.445 E.02433
G1 X118.299 Y119.085 E1.02506
G1 X117.649 Y119.085 E.02433
G1 X137.659 Y139.095 E1.05946
G1 X137.659 Y139.745 E.02433
G1 X117 Y119.085 E1.09386
G1 X116.35 Y119.085 E.02433
G1 X137.659 Y140.394 E1.12826
G1 X137.659 Y141.044 E.02433
G1 X115.7 Y119.085 E1.16267
G1 X115.05 Y119.085 E.02433
G1 X137.659 Y141.694 E1.19707
G1 X137.659 Y142.344 E.02432
G1 X114.401 Y119.085 E1.23147
G1 X114.401 Y119.734 E.02433
G1 X137.01 Y142.344 E1.19707
G1 X136.36 Y142.344 E.02433
G1 X114.401 Y120.384 E1.16266
G1 X114.401 Y121.034 E.02433
G1 X135.71 Y142.344 E1.12826
G1 X135.06 Y142.344 E.02433
G1 X114.401 Y121.684 E1.09386
G1 X114.401 Y122.333 E.02433
G1 X134.411 Y142.344 E1.05946
G1 X133.761 Y142.344 E.02433
G1 X114.401 Y122.983 E1.02506
G1 X114.401 Y123.633 E.02433
G1 X133.111 Y142.344 E.99065
G1 X132.461 Y142.344 E.02433
G1 X114.401 Y124.283 E.95625
G1 X114.401 Y124.932 E.02433
G1 X131.812 Y142.344 E.92185
G1 X131.162 Y142.344 E.02433
G1 X114.401 Y125.582 E.88745
G1 X114.401 Y126.232 E.02433
G1 X130.512 Y142.344 E.85305
G1 X129.862 Y142.344 E.02433
G1 X114.401 Y126.882 E.81865
G1 X114.401 Y127.531 E.02433
G1 X129.213 Y142.344 E.78424
G1 X128.563 Y142.344 E.02433
G1 X114.401 Y128.181 E.74984
G1 X114.401 Y128.831 E.02433
G1 X127.913 Y142.344 E.71544
G1 X127.263 Y142.344 E.02433
G1 X114.401 Y129.481 E.68104
G1 X114.401 Y130.13 E.02433
G1 X126.614 Y142.344 E.64664
G1 X125.964 Y142.344 E.02433
G1 X114.401 Y130.78 E.61223
G1 X114.401 Y131.43 E.02433
G1 X125.314 Y142.344 E.57783
G1 X124.664 Y142.344 E.02433
G1 X114.401 Y132.08 E.54343
G1 X114.401 Y132.729 E.02433
G1 X124.015 Y142.344 E.50903
G1 X123.365 Y142.344 E.02433
G1 X114.401 Y133.379 E.47463
G1 X114.401 Y134.029 E.02433
G1 X122.715 Y142.344 E.44023
G1 X122.065 Y142.344 E.02433
G1 X114.401 Y134.679 E.40582
G1 X114.401 Y135.328 E.02433
G1 X121.416 Y142.344 E.37142
G1 X120.766 Y142.344 E.02433
G1 X114.401 Y135.978 E.33702
G1 X114.401 Y136.628 E.02433
G1 X120.116 Y142.344 E.30262
G1 X119.466 Y142.344 E.02433
G1 X114.401 Y137.278 E.26822
G1 X114.401 Y137.927 E.02433
G1 X118.817 Y142.344 E.23381
G1 X118.167 Y142.344 E.02433
G1 X114.401 Y138.577 E.19941
G1 X114.401 Y139.227 E.02433
G1 X117.517 Y142.344 E.16501
G1 X116.867 Y142.344 E.02433
G1 X114.401 Y139.877 E.13061
G1 X114.401 Y140.526 E.02433
G1 X116.218 Y142.344 E.09621
G1 X115.568 Y142.344 E.02433
G1 X114.401 Y141.176 E.06181
G1 X114.401 Y141.826 E.02433
G1 X115.124 Y142.549 E.03829
; WIPE_START
G1 X114.401 Y141.826 E-.38868
G1 X114.401 Y141.176 E-.2469
G1 X114.632 Y141.408 E-.12442
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z0.6 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z0.6
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #1
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S1A
M204 S9000

G17
G2 Z0.6 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z3.2 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I0 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T1
M620.1 E F523 T240
M620.10 A1 F523 L119.736 H0.4 T240

G1 Y128 F9000




M620.11 S1 I0 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA



; WIPE
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
M106 P1 S0



M106 P1 S60
; FLUSH_START
G1 E10.7763 F523
G1 E1.19736 F50
M73 P2 R245
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
; FLUSH_END
G1 E-2 F1800
G1 E2 F300










M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z3.2
M106 P1 S0

M204 S500



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S1A
G392 S0

M1007 S1
M106 S0
M106 P2 S0
G1 Z3.6 F42000
G1 X15 Y221
G1 Z3.6
G1 Z.2
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X50.000 Y221.000  E1.3302
G1  Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000  E0.7791
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
; LINE_WIDTH: 0.575000
M73 P3 R245
G1  X49.500  E1.4860 F990
G1  Y222.000  E0.0219
G1  X15.500  E1.4860 F1125
G1  Y222.500  E0.0219
G1  X49.500  E1.4860 F1374
G1  Y223.000  E0.0219
G1  X15.500  E1.4860 F2625
G1  Y223.500  E0.0219
G1  X49.500  E1.4860 F2675
G1  Y224.000  E0.0219
G1  X15.500  E1.4860 F2725
G1  Y224.500  E0.0219
G1  X49.500  E1.4860 F2775
G1  Y225.000  E0.0219
G1  X15.500  E1.4860 F2825
G1  Y225.500  E0.0219
G1  X49.500  E1.4860 F2875
G1  Y226.000  E0.0219
G1  X15.500  E1.4860 F2925
G1  Y226.500  E0.0219
G1  X49.500  E1.4860 F2975
G1  Y227.000  E0.0219
G1  X15.500  E1.4860 F3000
G1  Y227.500  E0.0219
G1  X49.500  E1.4860
G1  Y228.000  E0.0219
G1  X15.500  E1.4860
G1  Y228.500  E0.0219
G1  X49.500  E1.4860
G1  Y229.000  E0.0219
G1  X15.500  E1.4860
G1  Y229.500  E0.0219
G1  X49.500  E1.4860
G1  Y230.000  E0.0219
G1  X15.500  E1.4860
G1  Y230.500  E0.0219
G1  X49.500  E1.4860
G1  Y231.000  E0.0219
G1  X15.500  E1.4860
G1  Y231.500  E0.0219
M73 P3 R244
G1  X49.500  E1.4860
G1  Y232.000  E0.0219
G1  X15.500  E1.4860
G1  Y232.500  E0.0219
G1  X49.500  E1.4860
G1  Y233.000  E0.0219
G1  X15.500  E1.4860
G1  Y233.500  E0.0219
G1  X49.500  E1.4860
G1  Y234.000  E0.0219
G1  X15.500  E1.4860
G1  Y234.500  E0.0219
G1  X49.500  E1.4860
G1  Y235.000  E0.0219
G1  X15.500  E1.4860
G1  Y235.500  E0.0219
G1  X49.500  E1.4860
G1  Y236.000  E0.0219
G1  X15.500  E1.4860
G1  Y236.500  E0.0219
G1  X49.500  E1.4860
G1  Y237.000  E0.0219
G1  X15.500  E1.4860
G1  Y237.500  E0.0219
G1  X49.500  E1.4860
G1  Y238.000  E0.0219
G1  X15.500  E1.4860
G1  Y238.500  E0.0219
G1  X49.500  E1.4860
G1  Y239.000  E0.0219
G1  X15.500  E1.4860
G1  Y239.500  E0.0219
G1  X49.500  E1.4860
G1  Y240.000  E0.0219
G1  X15.500  E1.4860
; LINE_WIDTH: 0.500000
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F3000
G1  Y240.500  E0.0190
G1  X49.500  E1.2922
G1  Y241.000  E0.0190
G1  X15.500  E1.2922
G1  X14.543 
G1  Y241.957 
G1  Y220.543  E0.8139
G1  X50.457  E1.3650
G1  Y241.957  E0.8139
G1  X14.543  E1.3650
G1  X14.086 
G1  Y242.414 
G1  Y220.086  E0.8486
G1  X50.914  E1.3997
G1  Y242.414  E0.8486
G1  X14.086  E1.3997
G1  X13.629 
G1  Y242.871 
G1  Y219.629  E0.8834
G1  X51.371  E1.4345
G1  Y242.871  E0.8834
G1  X13.629  E1.4345
G1  X13.172 
G1  Y243.328 
G1  Y219.172  E0.9181
G1  X51.828  E1.4692
G1  Y243.328  E0.9181
G1  X13.172  E1.4692
G1  X12.715 
G1  Y243.785 
G1  Y218.715  E0.9528
G1  X52.285  E1.5039
G1  Y243.785  E0.9528
G1  X12.715  E1.5039
G1  X12.258 
G1  Y244.242 
G1  Y218.258  E0.9876
G1  X52.742  E1.5387
G1  Y244.242  E0.9876
G1  X12.258  E1.5387
G1  X11.800 
G1  Y244.700 
G1  Y217.800  E1.0223
G1  X53.200  E1.5734
G1  Y244.700  E1.0223
G1  X11.800  E1.5734
; WIPE_TOWER_END

; WIPE_START
G1 F6300
G1 X11.8 Y242.7 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G1 X18.235 Y238.595 Z.6 F42000
G1 X169.056 Y142.377 Z.6
G1 Z.2
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.5
; LAYER_HEIGHT: 0.2
G1 F3000
G1 X145.021 Y142.377 E.89524
G1 X145.021 Y118.341 E.89524
G1 X169.056 Y118.341 E.89524
G1 X169.056 Y142.317 E.89301
G1 X169.514 Y142.834 F42000
; FEATURE: Outer wall
G1 F3000
G1 X144.564 Y142.834 E.92929
G1 X144.564 Y117.884 E.92929
G1 X169.514 Y117.884 E.92929
G1 X169.514 Y142.774 E.92706
; WIPE_START
G1 X167.514 Y142.779 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X167.649 Y135.148 Z.6 F42000
G1 X167.945 Y118.524 Z.6
G1 Z.2
G1 E.8 F1800
; FEATURE: Bottom surface
; LINE_WIDTH: 0.502362
G1 F6300
G1 X168.668 Y119.247 E.0383
G1 X168.668 Y119.897 E.02433
G1 X167.501 Y118.73 E.06181
G1 X166.851 Y118.73 E.02433
G1 X168.668 Y120.547 E.09621
G1 X168.668 Y121.197 E.02433
G1 X166.201 Y118.73 E.13061
G1 X165.551 Y118.73 E.02433
G1 X168.668 Y121.846 E.16501
G1 X168.668 Y122.496 E.02433
G1 X164.902 Y118.73 E.19941
G1 X164.252 Y118.73 E.02433
G1 X168.668 Y123.146 E.23382
G1 X168.668 Y123.796 E.02433
G1 X163.602 Y118.73 E.26822
G1 X162.952 Y118.73 E.02433
G1 X168.668 Y124.445 E.30262
G1 X168.668 Y125.095 E.02433
G1 X162.303 Y118.73 E.33702
G1 X161.653 Y118.73 E.02433
G1 X168.668 Y125.745 E.37142
G1 X168.668 Y126.395 E.02433
G1 X161.003 Y118.73 E.40583
G1 X160.353 Y118.73 E.02433
G1 X168.668 Y127.044 E.44023
G1 X168.668 Y127.694 E.02433
G1 X159.704 Y118.73 E.47463
G1 X159.054 Y118.73 E.02433
G1 X168.668 Y128.344 E.50903
G1 X168.668 Y128.994 E.02433
G1 X158.404 Y118.73 E.54343
G1 X157.754 Y118.73 E.02433
G1 X168.668 Y129.643 E.57784
G1 X168.668 Y130.293 E.02433
G1 X157.105 Y118.73 E.61224
G1 X156.455 Y118.73 E.02433
G1 X168.668 Y130.943 E.64664
G1 X168.668 Y131.593 E.02433
G1 X155.805 Y118.73 E.68104
G1 X155.155 Y118.73 E.02433
G1 X168.668 Y132.242 E.71544
G1 X168.668 Y132.892 E.02433
G1 X154.506 Y118.73 E.74984
G1 X153.856 Y118.73 E.02433
G1 X168.668 Y133.542 E.78425
G1 X168.668 Y134.192 E.02433
G1 X153.206 Y118.73 E.81865
G1 X152.556 Y118.73 E.02433
G1 X168.668 Y134.841 E.85305
G1 X168.668 Y135.491 E.02433
G1 X151.907 Y118.73 E.88745
G1 X151.257 Y118.73 E.02433
G1 X168.668 Y136.141 E.92185
G1 X168.668 Y136.791 E.02433
G1 X150.607 Y118.73 E.95626
G1 X149.957 Y118.73 E.02433
G1 X168.668 Y137.44 E.99066
G1 X168.668 Y138.09 E.02433
G1 X149.308 Y118.73 E1.02506
G1 X148.658 Y118.73 E.02433
G1 X168.668 Y138.74 E1.05946
G1 X168.668 Y139.39 E.02433
G1 X148.008 Y118.73 E1.09386
G1 X147.358 Y118.73 E.02433
G1 X168.668 Y140.039 E1.12826
G1 X168.668 Y140.689 E.02433
G1 X146.709 Y118.73 E1.16267
G1 X146.059 Y118.73 E.02433
G1 X168.668 Y141.339 E1.19707
G1 X168.668 Y141.989 E.02432
G1 X145.409 Y118.73 E1.23147
G1 X145.409 Y119.38 E.02433
G1 X168.018 Y141.989 E1.19707
G1 X167.368 Y141.989 E.02433
G1 X145.409 Y120.029 E1.16266
G1 X145.409 Y120.679 E.02433
M73 P3 R243
G1 X166.719 Y141.989 E1.12826
G1 X166.069 Y141.989 E.02433
G1 X145.409 Y121.329 E1.09386
G1 X145.409 Y121.979 E.02433
G1 X165.419 Y141.989 E1.05946
G1 X164.769 Y141.989 E.02433
G1 X145.409 Y122.628 E1.02506
G1 X145.409 Y123.278 E.02433
G1 X164.12 Y141.989 E.99065
G1 X163.47 Y141.989 E.02433
G1 X145.409 Y123.928 E.95625
G1 X145.409 Y124.578 E.02433
G1 X162.82 Y141.989 E.92185
G1 X162.17 Y141.989 E.02433
G1 X145.409 Y125.227 E.88745
G1 X145.409 Y125.877 E.02433
G1 X161.521 Y141.989 E.85305
G1 X160.871 Y141.989 E.02433
G1 X145.409 Y126.527 E.81865
G1 X145.409 Y127.177 E.02433
G1 X160.221 Y141.989 E.78424
G1 X159.571 Y141.989 E.02433
G1 X145.409 Y127.826 E.74984
G1 X145.409 Y128.476 E.02433
G1 X158.922 Y141.989 E.71544
G1 X158.272 Y141.989 E.02433
G1 X145.409 Y129.126 E.68104
G1 X145.409 Y129.776 E.02433
G1 X157.622 Y141.989 E.64664
G1 X156.972 Y141.989 E.02433
G1 X145.409 Y130.425 E.61223
G1 X145.409 Y131.075 E.02433
G1 X156.323 Y141.989 E.57783
G1 X155.673 Y141.989 E.02433
G1 X145.409 Y131.725 E.54343
G1 X145.409 Y132.375 E.02433
G1 X155.023 Y141.989 E.50903
G1 X154.373 Y141.989 E.02433
G1 X145.409 Y133.024 E.47463
G1 X145.409 Y133.674 E.02433
G1 X153.724 Y141.989 E.44023
G1 X153.074 Y141.989 E.02433
G1 X145.409 Y134.324 E.40582
G1 X145.409 Y134.974 E.02433
G1 X152.424 Y141.989 E.37142
G1 X151.774 Y141.989 E.02433
G1 X145.409 Y135.623 E.33702
G1 X145.409 Y136.273 E.02433
G1 X151.125 Y141.989 E.30262
G1 X150.475 Y141.989 E.02433
G1 X145.409 Y136.923 E.26822
G1 X145.409 Y137.573 E.02433
G1 X149.825 Y141.989 E.23381
G1 X149.175 Y141.989 E.02433
G1 X145.409 Y138.222 E.19941
G1 X145.409 Y138.872 E.02433
G1 X148.526 Y141.989 E.16501
G1 X147.876 Y141.989 E.02433
G1 X145.409 Y139.522 E.13061
G1 X145.409 Y140.172 E.02433
G1 X147.226 Y141.989 E.09621
G1 X146.576 Y141.989 E.02433
G1 X145.409 Y140.821 E.06181
G1 X145.409 Y141.471 E.02433
G1 X146.132 Y142.194 E.03829
; CHANGE_LAYER
; Z_HEIGHT: 0.4
; LAYER_HEIGHT: 0.2
; WIPE_START
G1 F6300
G1 X145.409 Y141.471 E-.38868
G1 X145.409 Y140.821 E-.2469
G1 X145.641 Y141.053 E-.12442
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
; layer num/total_layer_count: 2/128
; update layer progress
M73 L2
M991 S0 P1 ;notify layer change
M106 S165.75
M106 P2 S178
; open powerlost recovery
M1003 S1
M204 S6000
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G17
G3 Z.6 I-.077 J1.214 P1  F42000
G1 X169.236 Y142.557 Z.6
G1 Z.4
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X168.307 Y142.394 Z.8 F42000
G1 Z.4
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X168.903 Y141.797 E.02603
G1 X168.903 Y141.262 E.01654
G1 X167.941 Y142.224 E.042
G1 X167.405 Y142.224 E.01654
G1 X168.903 Y140.726 E.06539
G1 X168.903 Y140.19 E.01654
G1 X166.87 Y142.224 E.08877
G1 X166.334 Y142.224 E.01654
G1 X168.903 Y139.655 E.11216
G1 X168.903 Y139.119 E.01654
G1 X165.798 Y142.224 E.13554
G1 X165.263 Y142.224 E.01654
G1 X168.903 Y138.583 E.15893
G1 X168.903 Y138.047 E.01654
G1 X164.727 Y142.224 E.18231
G1 X164.191 Y142.224 E.01654
G1 X168.903 Y137.512 E.2057
G1 X168.903 Y136.976 E.01654
G1 X163.655 Y142.224 E.22908
G1 X163.12 Y142.224 E.01654
G1 X168.903 Y136.44 E.25246
G1 X168.903 Y135.905 E.01654
G1 X162.584 Y142.224 E.27585
G1 X162.048 Y142.224 E.01654
G1 X168.903 Y135.369 E.29923
G1 X168.903 Y134.833 E.01654
G1 X161.513 Y142.224 E.32262
G1 X160.977 Y142.224 E.01654
G1 X168.903 Y134.298 E.346
G1 X168.903 Y133.762 E.01654
G1 X160.441 Y142.224 E.36939
G1 X159.906 Y142.224 E.01654
G1 X168.903 Y133.226 E.39277
G1 X168.903 Y132.691 E.01654
G1 X159.37 Y142.224 E.41616
G1 X158.834 Y142.224 E.01654
G1 X168.903 Y132.155 E.43954
G1 X168.903 Y131.619 E.01654
G1 X158.298 Y142.224 E.46293
G1 X157.763 Y142.224 E.01654
G1 X168.903 Y131.083 E.48631
G1 X168.903 Y130.548 E.01654
G1 X157.227 Y142.224 E.50969
G1 X156.691 Y142.224 E.01654
G1 X168.903 Y130.012 E.53308
G1 X168.903 Y129.476 E.01654
G1 X156.156 Y142.224 E.55646
G1 X155.62 Y142.224 E.01654
G1 X168.903 Y128.941 E.57985
G1 X168.903 Y128.405 E.01654
G1 X155.084 Y142.224 E.60323
G1 X154.549 Y142.224 E.01654
G1 X168.903 Y127.869 E.62662
G1 X168.903 Y127.334 E.01654
G1 X154.013 Y142.224 E.65
G1 X153.477 Y142.224 E.01654
G1 X168.903 Y126.798 E.67339
G1 X168.903 Y126.262 E.01654
G1 X152.941 Y142.224 E.69677
G1 X152.406 Y142.224 E.01654
G1 X168.903 Y125.726 E.72016
G1 X168.903 Y125.191 E.01654
G1 X151.87 Y142.224 E.74354
G1 X151.334 Y142.224 E.01654
G1 X168.903 Y124.655 E.76692
G1 X168.903 Y124.119 E.01654
G1 X150.799 Y142.224 E.79031
G1 X150.263 Y142.224 E.01654
G1 X168.903 Y123.584 E.81369
G1 X168.903 Y123.048 E.01654
G1 X149.727 Y142.224 E.83708
G1 X149.192 Y142.224 E.01654
G1 X168.903 Y122.512 E.86046
G1 X168.903 Y121.977 E.01654
G1 X148.656 Y142.224 E.88385
G1 X148.12 Y142.224 E.01654
G1 X168.903 Y121.441 E.90723
G1 X168.903 Y120.905 E.01654
G1 X147.585 Y142.224 E.93062
G1 X147.049 Y142.224 E.01654
G1 X168.903 Y120.369 E.954
G1 X168.903 Y119.834 E.01654
G1 X146.513 Y142.224 E.97739
G1 X145.977 Y142.224 E.01654
G1 X168.903 Y119.298 E1.00077
G1 X168.903 Y118.762 E.01654
G1 X145.442 Y142.224 E1.02415
G1 X145.174 Y142.224 E.00827
G1 X145.174 Y141.956 E.00827
G1 X168.635 Y118.495 E1.02415
G1 X168.1 Y118.495 E.01654
G1 X145.174 Y141.42 E1.00077
G1 X145.174 Y140.885 E.01654
G1 X167.564 Y118.495 E.97738
G1 X167.028 Y118.495 E.01654
G1 X145.174 Y140.349 E.954
G1 X145.174 Y139.813 E.01654
G1 X166.493 Y118.495 E.93061
G1 X165.957 Y118.495 E.01654
G1 X145.174 Y139.278 E.90723
G1 X145.174 Y138.742 E.01654
G1 X165.421 Y118.495 E.88384
G1 X164.885 Y118.495 E.01654
G1 X145.174 Y138.206 E.86046
G1 X145.174 Y137.67 E.01654
G1 X164.35 Y118.495 E.83707
G1 X163.814 Y118.495 E.01654
G1 X145.174 Y137.135 E.81369
G1 X145.174 Y136.599 E.01654
G1 X163.278 Y118.495 E.7903
G1 X162.743 Y118.495 E.01654
G1 X145.174 Y136.063 E.76692
G1 X145.174 Y135.528 E.01654
G1 X162.207 Y118.495 E.74354
G1 X161.671 Y118.495 E.01654
G1 X145.174 Y134.992 E.72015
G1 X145.174 Y134.456 E.01654
G1 X161.136 Y118.495 E.69677
G1 X160.6 Y118.495 E.01654
G1 X145.174 Y133.921 E.67338
G1 X145.174 Y133.385 E.01654
G1 X160.064 Y118.495 E.65
G1 X159.528 Y118.495 E.01654
G1 X145.174 Y132.849 E.62661
G1 X145.174 Y132.313 E.01654
G1 X158.993 Y118.495 E.60323
G1 X158.457 Y118.495 E.01654
G1 X145.174 Y131.778 E.57984
G1 X145.174 Y131.242 E.01654
G1 X157.921 Y118.495 E.55646
G1 X157.386 Y118.495 E.01654
G1 X145.174 Y130.706 E.53308
G1 X145.174 Y130.171 E.01654
G1 X156.85 Y118.495 E.50969
G1 X156.314 Y118.495 E.01654
G1 X145.174 Y129.635 E.48631
G1 X145.174 Y129.099 E.01654
G1 X155.779 Y118.495 E.46292
G1 X155.243 Y118.495 E.01654
G1 X145.174 Y128.564 E.43954
G1 X145.174 Y128.028 E.01654
G1 X154.707 Y118.495 E.41615
G1 X154.172 Y118.495 E.01654
G1 X145.174 Y127.492 E.39277
G1 X145.174 Y126.956 E.01654
G1 X153.636 Y118.495 E.36938
G1 X153.1 Y118.495 E.01654
G1 X145.174 Y126.421 E.346
G1 X145.174 Y125.885 E.01654
G1 X152.564 Y118.495 E.32261
G1 X152.029 Y118.495 E.01654
G1 X145.174 Y125.349 E.29923
G1 X145.174 Y124.814 E.01654
G1 X151.493 Y118.495 E.27585
G1 X150.957 Y118.495 E.01654
G1 X145.174 Y124.278 E.25246
G1 X145.174 Y123.742 E.01654
G1 X150.422 Y118.495 E.22908
G1 X149.886 Y118.495 E.01654
G1 X145.174 Y123.207 E.20569
G1 X145.174 Y122.671 E.01654
G1 X149.35 Y118.495 E.18231
G1 X148.815 Y118.495 E.01654
G1 X145.174 Y122.135 E.15892
G1 X145.174 Y121.6 E.01654
G1 X148.279 Y118.495 E.13554
G1 X147.743 Y118.495 E.01654
G1 X145.174 Y121.064 E.11215
G1 X145.174 Y120.528 E.01654
G1 X147.207 Y118.495 E.08877
G1 X146.672 Y118.495 E.01654
G1 X145.174 Y119.992 E.06538
G1 X145.174 Y119.457 E.01654
G1 X146.136 Y118.495 E.042
G1 X145.6 Y118.495 E.01654
G1 X145.004 Y119.091 E.02602
; WIPE_START
G1 X145.6 Y118.495 E-.32036
G1 X146.136 Y118.495 E-.20356
G1 X145.697 Y118.934 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z0.8 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z0.8
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #2
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S0A
M204 S9000

G17
G2 Z0.8 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z3.4 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I1 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T0
M620.1 E F523 T240
M620.10 A1 F523 L82.3188 H0.4 T240

G1 Y128 F9000




M620.11 S1 I1 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA














M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M73 P4 R242
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z3.4
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S0A
G392 S0

M1007 S1
M106 S165.75
M106 P2 S178
G1 Z3.8 F42000
G1 X50 Y221
G1 Z3.8
G1 Z.4
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X50.000 Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000  E0.7791
G1  X50.000  E1.3302
G1  X15.000 
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
M73 P4 R241
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
G1  X50.457 
G1  Y241.957 
G1  X14.543  E1.3650
G1  Y220.543  E0.8139
G1  X50.457  E1.3650
G1  Y241.957  E0.8139
G1  X50.914 
G1  Y242.414 
G1  X14.086  E1.3997
G1  Y220.086  E0.8486
G1  X50.914  E1.3997
G1  Y242.414  E0.8486
G1  X51.371 
G1  Y242.871 
G1  X13.629  E1.4345
G1  Y219.629  E0.8834
G1  X51.371  E1.4345
G1  Y242.871  E0.8834
G1  X51.828 
G1  Y243.328 
G1  X13.172  E1.4692
G1  Y219.172  E0.9181
G1  X51.828  E1.4692
G1  Y243.328  E0.9181
G1  X52.285 
G1  Y243.785 
G1  X12.715  E1.5039
G1  Y218.715  E0.9528
G1  X52.285  E1.5039
G1  Y243.785  E0.9528
; WIPE_TOWER_END

; WIPE_START
G1 F15000
G1 X50.285 Y243.785 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G1 X55.301 Y238.032 Z.8 F42000
G1 X138.228 Y142.912 Z.8
G1 Z.4
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X137.298 Y142.748 Z.8 F42000
G1 Z.4
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X137.895 Y142.152 E.02603
G1 X137.895 Y141.617 E.01654
G1 X136.932 Y142.579 E.042
G1 X136.397 Y142.579 E.01654
G1 X137.895 Y141.081 E.06539
G1 X137.895 Y140.545 E.01654
G1 X135.861 Y142.579 E.08877
G1 X135.325 Y142.579 E.01654
G1 X137.895 Y140.009 E.11216
G1 X137.895 Y139.474 E.01654
G1 X134.79 Y142.579 E.13554
G1 X134.254 Y142.579 E.01654
G1 X137.895 Y138.938 E.15893
G1 X137.895 Y138.402 E.01654
G1 X133.718 Y142.579 E.18231
G1 X133.183 Y142.579 E.01654
G1 X137.895 Y137.867 E.2057
G1 X137.895 Y137.331 E.01654
G1 X132.647 Y142.579 E.22908
G1 X132.111 Y142.579 E.01654
G1 X137.895 Y136.795 E.25246
G1 X137.895 Y136.26 E.01654
G1 X131.575 Y142.579 E.27585
G1 X131.04 Y142.579 E.01654
G1 X137.895 Y135.724 E.29923
G1 X137.895 Y135.188 E.01654
G1 X130.504 Y142.579 E.32262
G1 X129.968 Y142.579 E.01654
G1 X137.895 Y134.652 E.346
G1 X137.895 Y134.117 E.01654
G1 X129.433 Y142.579 E.36939
G1 X128.897 Y142.579 E.01654
G1 X137.895 Y133.581 E.39277
G1 X137.895 Y133.045 E.01654
G1 X128.361 Y142.579 E.41616
G1 X127.826 Y142.579 E.01654
G1 X137.895 Y132.51 E.43954
G1 X137.895 Y131.974 E.01654
G1 X127.29 Y142.579 E.46293
G1 X126.754 Y142.579 E.01654
G1 X137.895 Y131.438 E.48631
G1 X137.895 Y130.903 E.01654
G1 X126.218 Y142.579 E.50969
G1 X125.683 Y142.579 E.01654
G1 X137.895 Y130.367 E.53308
G1 X137.895 Y129.831 E.01654
G1 X125.147 Y142.579 E.55646
G1 X124.611 Y142.579 E.01654
G1 X137.895 Y129.296 E.57985
G1 X137.895 Y128.76 E.01654
G1 X124.076 Y142.579 E.60323
G1 X123.54 Y142.579 E.01654
G1 X137.895 Y128.224 E.62662
G1 X137.895 Y127.688 E.01654
G1 X123.004 Y142.579 E.65
G1 X122.469 Y142.579 E.01654
G1 X137.895 Y127.153 E.67339
G1 X137.895 Y126.617 E.01654
G1 X121.933 Y142.579 E.69677
G1 X121.397 Y142.579 E.01654
G1 X137.895 Y126.081 E.72016
G1 X137.895 Y125.546 E.01654
G1 X120.862 Y142.579 E.74354
G1 X120.326 Y142.579 E.01654
G1 X137.895 Y125.01 E.76692
G1 X137.895 Y124.474 E.01654
G1 X119.79 Y142.579 E.79031
G1 X119.254 Y142.579 E.01654
G1 X137.895 Y123.939 E.81369
G1 X137.895 Y123.403 E.01654
G1 X118.719 Y142.579 E.83708
G1 X118.183 Y142.579 E.01654
G1 X137.895 Y122.867 E.86046
G1 X137.895 Y122.331 E.01654
G1 X117.647 Y142.579 E.88385
G1 X117.112 Y142.579 E.01654
G1 X137.895 Y121.796 E.90723
G1 X137.895 Y121.26 E.01654
G1 X116.576 Y142.579 E.93062
G1 X116.04 Y142.579 E.01654
G1 X137.895 Y120.724 E.954
G1 X137.895 Y120.189 E.01654
G1 X115.505 Y142.579 E.97739
G1 X114.969 Y142.579 E.01654
G1 X137.895 Y119.653 E1.00077
G1 X137.895 Y119.117 E.01654
G1 X114.433 Y142.579 E1.02415
G1 X114.165 Y142.579 E.00827
G1 X114.165 Y142.311 E.00827
G1 X137.627 Y118.849 E1.02415
G1 X137.091 Y118.849 E.01654
G1 X114.165 Y141.775 E1.00077
G1 X114.165 Y141.239 E.01654
G1 X136.555 Y118.849 E.97738
G1 X136.02 Y118.849 E.01654
G1 X114.165 Y140.704 E.954
G1 X114.165 Y140.168 E.01654
G1 X135.484 Y118.849 E.93061
G1 X134.948 Y118.849 E.01654
G1 X114.165 Y139.632 E.90723
G1 X114.165 Y139.097 E.01654
G1 X134.413 Y118.849 E.88384
G1 X133.877 Y118.849 E.01654
G1 X114.165 Y138.561 E.86046
G1 X114.165 Y138.025 E.01654
G1 X133.341 Y118.849 E.83707
G1 X132.805 Y118.849 E.01654
G1 X114.165 Y137.49 E.81369
G1 X114.165 Y136.954 E.01654
G1 X132.27 Y118.849 E.7903
G1 X131.734 Y118.849 E.01654
G1 X114.165 Y136.418 E.76692
G1 X114.165 Y135.883 E.01654
G1 X131.198 Y118.849 E.74354
G1 X130.663 Y118.849 E.01654
G1 X114.165 Y135.347 E.72015
G1 X114.165 Y134.811 E.01654
G1 X130.127 Y118.849 E.69677
G1 X129.591 Y118.849 E.01654
G1 X114.165 Y134.275 E.67338
G1 X114.165 Y133.74 E.01654
G1 X129.056 Y118.849 E.65
G1 X128.52 Y118.849 E.01654
G1 X114.165 Y133.204 E.62661
G1 X114.165 Y132.668 E.01654
G1 X127.984 Y118.849 E.60323
G1 X127.449 Y118.849 E.01654
G1 X114.165 Y132.133 E.57984
G1 X114.165 Y131.597 E.01654
G1 X126.913 Y118.849 E.55646
G1 X126.377 Y118.849 E.01654
G1 X114.165 Y131.061 E.53308
G1 X114.165 Y130.526 E.01654
G1 X125.841 Y118.849 E.50969
G1 X125.306 Y118.849 E.01654
G1 X114.165 Y129.99 E.48631
G1 X114.165 Y129.454 E.01654
G1 X124.77 Y118.849 E.46292
G1 X124.234 Y118.849 E.01654
G1 X114.165 Y128.918 E.43954
G1 X114.165 Y128.383 E.01654
G1 X123.699 Y118.849 E.41615
G1 X123.163 Y118.849 E.01654
G1 X114.165 Y127.847 E.39277
G1 X114.165 Y127.311 E.01654
G1 X122.627 Y118.849 E.36938
G1 X122.092 Y118.849 E.01654
G1 X114.165 Y126.776 E.346
G1 X114.165 Y126.24 E.01654
G1 X121.556 Y118.849 E.32261
G1 X121.02 Y118.849 E.01654
G1 X114.165 Y125.704 E.29923
G1 X114.165 Y125.169 E.01654
G1 X120.484 Y118.849 E.27585
G1 X119.949 Y118.849 E.01654
G1 X114.165 Y124.633 E.25246
G1 X114.165 Y124.097 E.01654
G1 X119.413 Y118.849 E.22908
G1 X118.877 Y118.849 E.01654
G1 X114.165 Y123.561 E.20569
G1 X114.165 Y123.026 E.01654
G1 X118.342 Y118.849 E.18231
G1 X117.806 Y118.849 E.01654
G1 X114.165 Y122.49 E.15892
G1 X114.165 Y121.954 E.01654
G1 X117.27 Y118.849 E.13554
G1 X116.735 Y118.849 E.01654
G1 X114.165 Y121.419 E.11215
G1 X114.165 Y120.883 E.01654
G1 X116.199 Y118.849 E.08877
G1 X115.663 Y118.849 E.01654
G1 X114.165 Y120.347 E.06538
G1 X114.165 Y119.812 E.01654
G1 X115.127 Y118.849 E.042
G1 X114.592 Y118.849 E.01654
G1 X113.996 Y119.446 E.02602
; CHANGE_LAYER
; Z_HEIGHT: 0.6
; LAYER_HEIGHT: 0.2
; WIPE_START
G1 F15000
G1 X114.592 Y118.849 E-.32036
G1 X115.127 Y118.849 E-.20356
G1 X114.688 Y119.289 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
; layer num/total_layer_count: 3/128
; update layer progress
M73 L3
M991 S0 P2 ;notify layer change
M106 S160.65
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G17
G3 Z.8 I-.862 J.859 P1  F42000
G1 X138.228 Y142.912 Z.8
G1 Z.6
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X137.082 Y135.63 Z1 F42000
G1 X138.064 Y119.446 Z1
G1 Z.6
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X137.468 Y118.849 E.02603
G1 X136.932 Y118.849 E.01654
G1 X137.895 Y119.812 E.042
G1 X137.895 Y120.347 E.01654
G1 X136.397 Y118.849 E.06539
G1 X135.861 Y118.849 E.01654
G1 X137.895 Y120.883 E.08877
G1 X137.895 Y121.419 E.01654
G1 X135.325 Y118.849 E.11216
G1 X134.79 Y118.849 E.01654
G1 X137.895 Y121.954 E.13554
G1 X137.895 Y122.49 E.01654
G1 X134.254 Y118.849 E.15893
G1 X133.718 Y118.849 E.01654
G1 X137.895 Y123.026 E.18231
G1 X137.895 Y123.562 E.01654
G1 X133.183 Y118.849 E.2057
G1 X132.647 Y118.849 E.01654
G1 X137.895 Y124.097 E.22908
G1 X137.895 Y124.633 E.01654
G1 X132.111 Y118.849 E.25246
G1 X131.575 Y118.849 E.01654
G1 X137.895 Y125.169 E.27585
G1 X137.895 Y125.704 E.01654
G1 X131.04 Y118.849 E.29923
G1 X130.504 Y118.849 E.01654
G1 X137.895 Y126.24 E.32262
G1 X137.895 Y126.776 E.01654
G1 X129.968 Y118.849 E.346
G1 X129.433 Y118.849 E.01654
G1 X137.895 Y127.311 E.36939
G1 X137.895 Y127.847 E.01654
G1 X128.897 Y118.849 E.39277
G1 X128.361 Y118.849 E.01654
G1 X137.895 Y128.383 E.41616
G1 X137.895 Y128.919 E.01654
G1 X127.826 Y118.849 E.43954
G1 X127.29 Y118.849 E.01654
G1 X137.895 Y129.454 E.46293
G1 X137.895 Y129.99 E.01654
G1 X126.754 Y118.849 E.48631
G1 X126.218 Y118.849 E.01654
G1 X137.895 Y130.526 E.50969
G1 X137.895 Y131.061 E.01654
G1 X125.683 Y118.849 E.53308
G1 X125.147 Y118.849 E.01654
G1 X137.895 Y131.597 E.55646
G1 X137.895 Y132.133 E.01654
G1 X124.611 Y118.849 E.57985
G1 X124.076 Y118.849 E.01654
G1 X137.895 Y132.668 E.60323
G1 X137.895 Y133.204 E.01654
G1 X123.54 Y118.849 E.62662
G1 X123.004 Y118.849 E.01654
G1 X137.895 Y133.74 E.65
G1 X137.895 Y134.276 E.01654
G1 X122.469 Y118.849 E.67339
G1 X121.933 Y118.849 E.01654
G1 X137.895 Y134.811 E.69677
G1 X137.895 Y135.347 E.01654
G1 X121.397 Y118.849 E.72016
G1 X120.862 Y118.849 E.01654
G1 X137.895 Y135.883 E.74354
G1 X137.895 Y136.418 E.01654
G1 X120.326 Y118.849 E.76692
G1 X119.79 Y118.849 E.01654
G1 X137.895 Y136.954 E.79031
G1 X137.895 Y137.49 E.01654
G1 X119.254 Y118.849 E.81369
G1 X118.719 Y118.849 E.01654
G1 X137.895 Y138.025 E.83708
G1 X137.895 Y138.561 E.01654
G1 X118.183 Y118.849 E.86046
G1 X117.647 Y118.849 E.01654
G1 X137.895 Y139.097 E.88385
G1 X137.895 Y139.632 E.01654
G1 X117.112 Y118.849 E.90723
G1 X116.576 Y118.849 E.01654
G1 X137.895 Y140.168 E.93062
G1 X137.895 Y140.704 E.01654
G1 X116.04 Y118.849 E.954
G1 X115.505 Y118.849 E.01654
G1 X137.895 Y141.24 E.97739
G1 X137.895 Y141.775 E.01654
G1 X114.969 Y118.849 E1.00077
G1 X114.433 Y118.849 E.01654
G1 X137.895 Y142.311 E1.02415
G1 X137.895 Y142.579 E.00827
G1 X137.627 Y142.579 E.00827
G1 X114.165 Y119.117 E1.02415
G1 X114.165 Y119.653 E.01654
G1 X137.091 Y142.579 E1.00077
G1 X136.555 Y142.579 E.01654
G1 X114.165 Y120.189 E.97738
G1 X114.165 Y120.724 E.01654
G1 X136.02 Y142.579 E.954
G1 X135.484 Y142.579 E.01654
G1 X114.165 Y121.26 E.93061
G1 X114.165 Y121.796 E.01654
G1 X134.948 Y142.579 E.90723
G1 X134.413 Y142.579 E.01654
G1 X114.165 Y122.332 E.88384
G1 X114.165 Y122.867 E.01654
G1 X133.877 Y142.579 E.86046
G1 X133.341 Y142.579 E.01654
G1 X114.165 Y123.403 E.83707
G1 X114.165 Y123.939 E.01654
G1 X132.805 Y142.579 E.81369
G1 X132.27 Y142.579 E.01654
G1 X114.165 Y124.474 E.7903
G1 X114.165 Y125.01 E.01654
G1 X131.734 Y142.579 E.76692
G1 X131.198 Y142.579 E.01654
G1 X114.165 Y125.546 E.74354
G1 X114.165 Y126.081 E.01654
G1 X130.663 Y142.579 E.72015
G1 X130.127 Y142.579 E.01654
G1 X114.165 Y126.617 E.69677
G1 X114.165 Y127.153 E.01654
G1 X129.591 Y142.579 E.67338
G1 X129.056 Y142.579 E.01654
G1 X114.165 Y127.689 E.65
G1 X114.165 Y128.224 E.01654
G1 X128.52 Y142.579 E.62661
G1 X127.984 Y142.579 E.01654
G1 X114.165 Y128.76 E.60323
G1 X114.165 Y129.296 E.01654
G1 X127.449 Y142.579 E.57984
G1 X126.913 Y142.579 E.01654
G1 X114.165 Y129.831 E.55646
G1 X114.165 Y130.367 E.01654
G1 X126.377 Y142.579 E.53308
G1 X125.841 Y142.579 E.01654
G1 X114.165 Y130.903 E.50969
G1 X114.165 Y131.438 E.01654
G1 X125.306 Y142.579 E.48631
G1 X124.77 Y142.579 E.01654
G1 X114.165 Y131.974 E.46292
G1 X114.165 Y132.51 E.01654
G1 X124.234 Y142.579 E.43954
G1 X123.699 Y142.579 E.01654
G1 X114.165 Y133.045 E.41615
G1 X114.165 Y133.581 E.01654
G1 X123.163 Y142.579 E.39277
G1 X122.627 Y142.579 E.01654
G1 X114.165 Y134.117 E.36938
G1 X114.165 Y134.653 E.01654
G1 X122.092 Y142.579 E.346
G1 X121.556 Y142.579 E.01654
G1 X114.165 Y135.188 E.32261
G1 X114.165 Y135.724 E.01654
G1 X121.02 Y142.579 E.29923
G1 X120.484 Y142.579 E.01654
G1 X114.165 Y136.26 E.27585
G1 X114.165 Y136.795 E.01654
G1 X119.949 Y142.579 E.25246
G1 X119.413 Y142.579 E.01654
G1 X114.165 Y137.331 E.22908
G1 X114.165 Y137.867 E.01654
G1 X118.877 Y142.579 E.20569
G1 X118.342 Y142.579 E.01654
G1 X114.165 Y138.402 E.18231
G1 X114.165 Y138.938 E.01654
G1 X117.806 Y142.579 E.15892
G1 X117.27 Y142.579 E.01654
G1 X114.165 Y139.474 E.13554
G1 X114.165 Y140.01 E.01654
G1 X116.735 Y142.579 E.11215
G1 X116.199 Y142.579 E.01654
G1 X114.165 Y140.545 E.08877
G1 X114.165 Y141.081 E.01654
G1 X115.663 Y142.579 E.06538
G1 X115.127 Y142.579 E.01654
G1 X114.165 Y141.617 E.042
G1 X114.165 Y142.152 E.01654
G1 X114.761 Y142.748 E.02602
; WIPE_START
G1 X114.165 Y142.152 E-.32036
G1 X114.165 Y141.617 E-.20356
G1 X114.605 Y142.056 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z1 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z1
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer

      M400
      G90
      M83
      M204 S5000
      G0 Z2 F4000
      G0 X261 Y250 F20000
      M400 P200
      G39 S1
      G0 Z2 F4000



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #3
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S1A
M204 S9000

G17
G2 Z1 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z3.6 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I0 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T1
M620.1 E F523 T240
M620.10 A1 F523 L119.736 H0.4 T240

G1 Y128 F9000




M620.11 S1 I0 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA



; WIPE
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
M106 P1 S0



M106 P1 S60
; FLUSH_START
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
M73 P4 R240
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
; FLUSH_END
G1 E-2 F1800
M73 P5 R240
G1 E2 F300










M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z3.6
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
M73 P5 R239
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S1A
G392 S0

M1007 S1
M106 S160.65
M106 P2 S178
G1 Z4 F42000
G1 X50 Y241.5
G1 Z4
G1 Z.6
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X15.000 Y241.500  E1.3302
G1  Y221.000  E0.7791
G1  X50.000  E1.3302
G1  Y241.500  E0.7791
G1  X15.000 Y221.000  
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
G1  X50.457 
G1  Y241.957 
G1  X14.543  E1.3650
G1  Y220.543  E0.8139
G1  X50.457  E1.3650
G1  Y241.957  E0.8139
G1  X50.914 
G1  Y242.414 
G1  X14.086  E1.3997
G1  Y220.086  E0.8486
G1  X50.914  E1.3997
G1  Y242.414  E0.8486
G1  X51.371 
G1  Y242.871 
G1  X13.629  E1.4345
G1  Y219.629  E0.8834
G1  X51.371  E1.4345
G1  Y242.871  E0.8834
G1  X51.828 
G1  Y243.328 
G1  X13.172  E1.4692
G1  Y219.172  E0.9181
G1  X51.828  E1.4692
G1  Y243.328  E0.9181
; WIPE_TOWER_END

; WIPE_START
G1 F15000
G1 X49.828 Y243.328 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G1 X55.661 Y238.406 Z1 F42000
G1 X169.236 Y142.557 Z1
G1 Z.6
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X168.091 Y135.276 Z1 F42000
G1 X169.073 Y119.091 Z1
G1 Z.6
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X168.477 Y118.495 E.02603
G1 X167.941 Y118.495 E.01654
G1 X168.903 Y119.457 E.042
G1 X168.903 Y119.993 E.01654
G1 X167.405 Y118.495 E.06539
G1 X166.87 Y118.495 E.01654
G1 X168.903 Y120.528 E.08877
G1 X168.903 Y121.064 E.01654
G1 X166.334 Y118.495 E.11216
G1 X165.798 Y118.495 E.01654
G1 X168.903 Y121.6 E.13554
G1 X168.903 Y122.135 E.01654
G1 X165.263 Y118.495 E.15893
G1 X164.727 Y118.495 E.01654
G1 X168.903 Y122.671 E.18231
G1 X168.903 Y123.207 E.01654
G1 X164.191 Y118.495 E.2057
G1 X163.655 Y118.495 E.01654
G1 X168.903 Y123.742 E.22908
G1 X168.903 Y124.278 E.01654
G1 X163.12 Y118.495 E.25246
G1 X162.584 Y118.495 E.01654
G1 X168.903 Y124.814 E.27585
G1 X168.903 Y125.349 E.01654
G1 X162.048 Y118.495 E.29923
G1 X161.513 Y118.495 E.01654
G1 X168.903 Y125.885 E.32262
G1 X168.903 Y126.421 E.01654
G1 X160.977 Y118.495 E.346
G1 X160.441 Y118.495 E.01654
G1 X168.903 Y126.957 E.36939
G1 X168.903 Y127.492 E.01654
G1 X159.906 Y118.495 E.39277
G1 X159.37 Y118.495 E.01654
G1 X168.903 Y128.028 E.41616
G1 X168.903 Y128.564 E.01654
G1 X158.834 Y118.495 E.43954
G1 X158.298 Y118.495 E.01654
G1 X168.903 Y129.099 E.46293
G1 X168.903 Y129.635 E.01654
G1 X157.763 Y118.495 E.48631
G1 X157.227 Y118.495 E.01654
G1 X168.903 Y130.171 E.50969
G1 X168.903 Y130.706 E.01654
G1 X156.691 Y118.495 E.53308
G1 X156.156 Y118.495 E.01654
G1 X168.903 Y131.242 E.55646
G1 X168.903 Y131.778 E.01654
G1 X155.62 Y118.495 E.57985
G1 X155.084 Y118.495 E.01654
G1 X168.903 Y132.314 E.60323
G1 X168.903 Y132.849 E.01654
G1 X154.549 Y118.495 E.62662
G1 X154.013 Y118.495 E.01654
G1 X168.903 Y133.385 E.65
G1 X168.903 Y133.921 E.01654
G1 X153.477 Y118.495 E.67339
G1 X152.941 Y118.495 E.01654
G1 X168.903 Y134.456 E.69677
G1 X168.903 Y134.992 E.01654
G1 X152.406 Y118.495 E.72016
G1 X151.87 Y118.495 E.01654
G1 X168.903 Y135.528 E.74354
G1 X168.903 Y136.063 E.01654
G1 X151.334 Y118.495 E.76692
G1 X150.799 Y118.495 E.01654
G1 X168.903 Y136.599 E.79031
G1 X168.903 Y137.135 E.01654
G1 X150.263 Y118.495 E.81369
G1 X149.727 Y118.495 E.01654
G1 X168.903 Y137.671 E.83708
G1 X168.903 Y138.206 E.01654
G1 X149.192 Y118.495 E.86046
G1 X148.656 Y118.495 E.01654
G1 X168.903 Y138.742 E.88385
G1 X168.903 Y139.278 E.01654
G1 X148.12 Y118.495 E.90723
G1 X147.585 Y118.495 E.01654
G1 X168.903 Y139.813 E.93062
G1 X168.903 Y140.349 E.01654
G1 X147.049 Y118.495 E.954
G1 X146.513 Y118.495 E.01654
G1 X168.903 Y140.885 E.97739
G1 X168.903 Y141.42 E.01654
G1 X145.977 Y118.495 E1.00077
G1 X145.442 Y118.495 E.01654
G1 X168.903 Y141.956 E1.02415
G1 X168.903 Y142.224 E.00827
G1 X168.635 Y142.224 E.00827
G1 X145.174 Y118.762 E1.02415
G1 X145.174 Y119.298 E.01654
G1 X168.1 Y142.224 E1.00077
G1 X167.564 Y142.224 E.01654
G1 X145.174 Y119.834 E.97738
G1 X145.174 Y120.37 E.01654
G1 X167.028 Y142.224 E.954
G1 X166.493 Y142.224 E.01654
G1 X145.174 Y120.905 E.93061
G1 X145.174 Y121.441 E.01654
G1 X165.957 Y142.224 E.90723
G1 X165.421 Y142.224 E.01654
G1 X145.174 Y121.977 E.88384
G1 X145.174 Y122.512 E.01654
G1 X164.885 Y142.224 E.86046
G1 X164.35 Y142.224 E.01654
G1 X145.174 Y123.048 E.83707
G1 X145.174 Y123.584 E.01654
G1 X163.814 Y142.224 E.81369
G1 X163.278 Y142.224 E.01654
G1 X145.174 Y124.119 E.7903
G1 X145.174 Y124.655 E.01654
G1 X162.743 Y142.224 E.76692
G1 X162.207 Y142.224 E.01654
G1 X145.174 Y125.191 E.74354
G1 X145.174 Y125.727 E.01654
G1 X161.671 Y142.224 E.72015
G1 X161.136 Y142.224 E.01654
G1 X145.174 Y126.262 E.69677
G1 X145.174 Y126.798 E.01654
G1 X160.6 Y142.224 E.67338
G1 X160.064 Y142.224 E.01654
G1 X145.174 Y127.334 E.65
G1 X145.174 Y127.869 E.01654
G1 X159.528 Y142.224 E.62661
G1 X158.993 Y142.224 E.01654
G1 X145.174 Y128.405 E.60323
G1 X145.174 Y128.941 E.01654
G1 X158.457 Y142.224 E.57984
G1 X157.921 Y142.224 E.01654
G1 X145.174 Y129.476 E.55646
G1 X145.174 Y130.012 E.01654
G1 X157.386 Y142.224 E.53308
G1 X156.85 Y142.224 E.01654
G1 X145.174 Y130.548 E.50969
G1 X145.174 Y131.084 E.01654
G1 X156.314 Y142.224 E.48631
G1 X155.779 Y142.224 E.01654
G1 X145.174 Y131.619 E.46292
G1 X145.174 Y132.155 E.01654
G1 X155.243 Y142.224 E.43954
G1 X154.707 Y142.224 E.01654
G1 X145.174 Y132.691 E.41615
G1 X145.174 Y133.226 E.01654
G1 X154.172 Y142.224 E.39277
G1 X153.636 Y142.224 E.01654
G1 X145.174 Y133.762 E.36938
; TOOLPATH OMITTED FROM TEST FIXTURE
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z24.8 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z24.8
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z24.8 F4000
            G39.3 S1
            G0 Z24.8 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #122
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S0A
M204 S9000

G17
G2 Z24.8 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z27.4 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I1 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T0
M620.1 E F523 T240
M620.10 A1 F523 L82.3188 H0.4 T240

G1 Y128 F9000




M620.11 S1 I1 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA














M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z27.4
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S0A
G392 S0

M1007 S1
M106 S178.5
M106 P2 S178
G1 Z27.8 F42000
G1 X50 Y221
G1 Z27.8
G1 Z24.4
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X50.000 Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000  E0.7791
G1  X50.000  E1.3302
G1  X15.000 
M73 P93 R16
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F15476.087
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G1 X52.973 Y234.413 Z24.8 F42000
G1 X138.228 Y142.912 Z24.8
G1 Z24.4
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
M73 P93 R15
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X136.295 Y135.623 Z24.8 F42000
G1 X135.58 Y118.864 Z24.8
G1 Z24.4
G1 E.8 F1800
; FEATURE: Sparse infill
; LINE_WIDTH: 0.45
G1 F15476.087
G1 X137.208 Y118.864 E.05401
G1 X137.88 Y119.536 E.0315
G1 X137.88 Y118.864 E.02227
G1 X114.18 Y142.564 E1.11178
G1 X114.18 Y141.892 E.02227
G1 X114.852 Y142.564 E.0315
G1 X121.856 Y142.564 E.23235
G1 X137.88 Y126.54 E.75169
G1 X137.88 Y127.212 E.02227
G1 X129.532 Y118.864 E.39159
G1 X130.204 Y118.864 E.02227
G1 X114.18 Y134.888 E.75169
G1 X114.18 Y134.216 E.02227
G1 X122.528 Y142.564 E.39159
G1 X129.532 Y142.564 E.23235
G1 X137.88 Y134.216 E.39159
G1 X137.88 Y134.888 E.02227
G1 X121.856 Y118.864 E.75169
G1 X122.528 Y118.864 E.02227
G1 X114.18 Y127.212 E.39159
G1 X114.18 Y126.54 E.02227
G1 X130.204 Y142.564 E.75169
G1 X131.832 Y142.564 E.05401
G1 X114.18 Y121.164 F42000
G1 F15476.087
G1 X114.18 Y119.536 E.05401
G1 X114.852 Y118.864 E.0315
G1 X114.18 Y118.864 E.02227
G1 X137.88 Y142.564 E1.11178
G1 X137.208 Y142.564 E.02227
G1 X137.88 Y141.892 E.0315
G1 X137.88 Y140.264 E.05401
; CHANGE_LAYER
; Z_HEIGHT: 24.6
; LAYER_HEIGHT: 0.200001
; WIPE_START
G1 F15476.087
G1 X137.88 Y141.892 E-.61876
G1 X137.617 Y142.155 E-.14124
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
; layer num/total_layer_count: 123/128
; update layer progress
M73 L123
M991 S0 P122 ;notify layer change
M106 S170.85
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G17
G3 Z24.8 I-.947 J.764 P1  F42000
G1 X138.228 Y142.912 Z24.8
G1 Z24.6
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X136.383 Y135.62 Z25 F42000
G1 X135.874 Y119.242 Z25
G1 Z24.6
G1 E.8 F1800
; FEATURE: Sparse infill
; LINE_WIDTH: 0.45
G1 F15476.087
G1 X137.503 Y119.242 E.05401
G1 X114.557 Y142.187 E1.0764
G1 X114.557 Y134.593 E.25188
G1 X122.151 Y142.187 E.35621
G1 X122.233 Y142.187 E.00275
G1 X137.503 Y126.918 E.71631
G1 X137.503 Y126.835 E.00275
G1 X129.909 Y119.242 E.35621
G1 X129.827 Y119.242 E.00275
G1 X114.557 Y134.511 E.71631
G1 X114.557 Y126.918 E.25188
G1 X129.827 Y142.187 E.71631
G1 X129.909 Y142.187 E.00275
G1 X137.503 Y134.593 E.35621
G1 X137.503 Y134.511 E.00275
G1 X122.233 Y119.242 E.71631
G1 X122.151 Y119.242 E.00275
G1 X114.557 Y126.835 E.35621
G1 X114.557 Y119.242 E.25188
G1 X137.503 Y142.187 E1.0764
G1 X135.874 Y142.187 E.05401
G1 X137.865 Y119.038 F42000
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.383108
G1 F15000
G1 X137.838 Y118.906 E.00373
G1 X137.706 Y118.879 E.00373
G1 X114.354 Y118.879 E.64735
G1 X114.222 Y118.906 E.00373
G1 X114.195 Y119.038 E.00373
G1 X114.195 Y142.39 E.64735
G1 X114.222 Y142.522 E.00373
G1 X114.354 Y142.549 E.00373
G1 X137.706 Y142.549 E.64735
G1 X137.838 Y142.522 E.00373
G1 X137.865 Y142.39 E.00373
G1 X137.865 Y119.098 E.64568
; WIPE_START
G1 X137.865 Y121.098 E-.76
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z25 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z25
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z25 F4000
            G39.3 S1
            G0 Z25 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #123
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S1A
M204 S9000

G17
G2 Z25 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z27.6 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I0 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T1
M620.1 E F523 T240
M620.10 A1 F523 L119.736 H0.4 T240

G1 Y128 F9000




M620.11 S1 I0 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA



; WIPE
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
M106 P1 S0



M106 P1 S60
; FLUSH_START
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
; FLUSH_END
G1 E-2 F1800
G1 E2 F300










M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z27.6
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S1A
G392 S0

M1007 S1
M106 S170.85
M106 P2 S178
G1 Z28 F42000
G1 X50 Y241.5
G1 Z28
G1 Z24.6
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X15.000 Y241.500  E1.3302
G1  Y221.000  E0.7791
G1  X50.000  E1.3302
M73 P94 R14
G1  Y241.500  E0.7791
G1  X15.000 Y221.000  
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F15000
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G1 X53.723 Y235.221 Z25 F42000
G1 X169.236 Y142.557 Z25
G1 Z24.6
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
M73 P94 R13
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X167.392 Y135.265 Z25 F42000
G1 X166.883 Y118.887 Z25
G1 Z24.6
G1 E.8 F1800
; FEATURE: Sparse infill
; LINE_WIDTH: 0.45
G1 F15476.087
G1 X168.511 Y118.887 E.05401
G1 X145.566 Y141.832 E1.0764
G1 X145.566 Y134.239 E.25188
G1 X153.159 Y141.832 E.35621
G1 X153.242 Y141.832 E.00275
G1 X168.511 Y126.563 E.71631
G1 X168.511 Y126.48 E.00275
G1 X160.918 Y118.887 E.35621
G1 X160.835 Y118.887 E.00275
G1 X145.566 Y134.156 E.71631
G1 X145.566 Y126.563 E.25188
G1 X160.835 Y141.832 E.71631
G1 X160.918 Y141.832 E.00275
G1 X168.511 Y134.239 E.35621
G1 X168.511 Y134.156 E.00275
G1 X153.242 Y118.887 E.71631
G1 X153.159 Y118.887 E.00275
G1 X145.566 Y126.48 E.35621
G1 X145.566 Y118.887 E.25188
G1 X168.511 Y141.832 E1.0764
G1 X166.883 Y141.832 E.05401
G1 X168.874 Y118.683 F42000
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.383108
G1 F15000
G1 X168.847 Y118.551 E.00373
G1 X168.715 Y118.524 E.00373
G1 X145.362 Y118.524 E.64735
G1 X145.231 Y118.551 E.00373
G1 X145.203 Y118.683 E.00373
G1 X145.203 Y142.035 E.64735
G1 X145.231 Y142.167 E.00373
G1 X145.362 Y142.194 E.00373
G1 X168.715 Y142.194 E.64735
G1 X168.847 Y142.167 E.00373
G1 X168.874 Y142.035 E.00373
G1 X168.874 Y118.743 E.64568
; CHANGE_LAYER
; Z_HEIGHT: 24.8
; LAYER_HEIGHT: 0.199999
; WIPE_START
G1 F15000
G1 X168.874 Y120.743 E-.76
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
; layer num/total_layer_count: 124/128
; update layer progress
M73 L124
M991 S0 P123 ;notify layer change
M106 S153
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G17
G3 Z25 I-1.217 J.02 P1  F42000
G1 X169.236 Y142.557 Z25
G1 Z24.8
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X168.15 Y142.39 Z25.2 F42000
G1 Z24.8
G1 E.8 F1800
; FEATURE: Bridge
; LINE_WIDTH: 0.404554
; LAYER_HEIGHT: 0.4
M106 S255
G1 F3000
G1 X168.867 Y141.673 E.05309
G1 X168.867 Y141.03 E.03367
G1 X167.71 Y142.187 E.08571
G1 X167.067 Y142.187 E.03367
G1 X168.867 Y140.387 E.13332
G1 X168.867 Y139.745 E.03367
G1 X166.424 Y142.187 E.18093
G1 X165.781 Y142.187 E.03367
G1 X168.867 Y139.102 E.22854
G1 X168.867 Y138.459 E.03367
G1 X165.138 Y142.187 E.27616
G1 X164.495 Y142.187 E.03367
G1 X168.867 Y137.816 E.32377
G1 X168.867 Y137.173 E.03367
G1 X163.853 Y142.187 E.37138
G1 X163.21 Y142.187 E.03367
G1 X168.867 Y136.53 E.41899
G1 X168.867 Y135.888 E.03367
G1 X162.567 Y142.187 E.4666
G1 X161.924 Y142.187 E.03367
G1 X168.867 Y135.245 E.51422
G1 X168.867 Y134.602 E.03367
G1 X161.281 Y142.187 E.56183
G1 X160.638 Y142.187 E.03367
G1 X168.867 Y133.959 E.60944
G1 X168.867 Y133.316 E.03367
G1 X159.996 Y142.187 E.65705
G1 X159.353 Y142.187 E.03367
G1 X168.867 Y132.673 E.70467
G1 X168.867 Y132.031 E.03367
G1 X158.71 Y142.187 E.75228
G1 X158.067 Y142.187 E.03367
G1 X168.867 Y131.388 E.79989
G1 X168.867 Y130.745 E.03367
G1 X157.424 Y142.187 E.8475
G1 X156.781 Y142.187 E.03367
G1 X168.867 Y130.102 E.89511
G1 X168.867 Y129.459 E.03367
G1 X156.139 Y142.187 E.94273
G1 X155.496 Y142.187 E.03367
G1 X168.867 Y128.816 E.99034
G1 X168.867 Y128.174 E.03367
G1 X154.853 Y142.187 E1.03795
G1 X154.21 Y142.187 E.03367
G1 X168.867 Y127.531 E1.08556
G1 X168.867 Y126.888 E.03367
G1 X153.567 Y142.187 E1.13318
G1 X152.924 Y142.187 E.03367
G1 X168.867 Y126.245 E1.18079
G1 X168.867 Y125.602 E.03367
G1 X152.282 Y142.187 E1.2284
G1 X151.639 Y142.187 E.03367
G1 X168.867 Y124.959 E1.27601
G1 X168.867 Y124.317 E.03367
G1 X150.996 Y142.187 E1.32362
G1 X150.353 Y142.187 E.03367
G1 X168.867 Y123.674 E1.37124
G1 X168.867 Y123.031 E.03367
G1 X149.71 Y142.187 E1.41885
G1 X149.067 Y142.187 E.03367
G1 X168.867 Y122.388 E1.46646
G1 X168.867 Y121.745 E.03367
G1 X148.425 Y142.187 E1.51407
G1 X147.782 Y142.187 E.03367
G1 X168.867 Y121.102 E1.56169
G1 X168.867 Y120.46 E.03367
G1 X147.139 Y142.187 E1.6093
G1 X146.496 Y142.187 E.03367
G1 X168.867 Y119.817 E1.65691
G1 X168.867 Y119.174 E.03367
G1 X145.853 Y142.187 E1.70452
G1 X145.21 Y142.187 E.03366
G1 X168.867 Y118.531 E1.75213
G1 X168.224 Y118.531 E.03367
G1 X145.21 Y141.545 E1.70451
G1 X145.21 Y140.902 E.03367
G1 X167.581 Y118.531 E1.6569
G1 X166.938 Y118.531 E.03367
G1 X145.21 Y140.259 E1.60929
G1 X145.21 Y139.616 E.03367
G1 X166.295 Y118.531 E1.56168
G1 X165.653 Y118.531 E.03367
G1 X145.21 Y138.973 E1.51407
G1 X145.21 Y138.33 E.03367
G1 X165.01 Y118.531 E1.46645
G1 X164.367 Y118.531 E.03367
M73 P94 R12
G1 X145.21 Y137.688 E1.41884
G1 X145.21 Y137.045 E.03367
G1 X163.724 Y118.531 E1.37123
G1 X163.081 Y118.531 E.03367
G1 X145.21 Y136.402 E1.32362
G1 X145.21 Y135.759 E.03367
G1 X162.438 Y118.531 E1.276
G1 X161.796 Y118.531 E.03367
G1 X145.21 Y135.116 E1.22839
G1 X145.21 Y134.473 E.03367
G1 X161.153 Y118.531 E1.18078
M73 P95 R12
G1 X160.51 Y118.531 E.03367
G1 X145.21 Y133.831 E1.13317
G1 X145.21 Y133.188 E.03367
G1 X159.867 Y118.531 E1.08556
G1 X159.224 Y118.531 E.03367
G1 X145.21 Y132.545 E1.03794
G1 X145.21 Y131.902 E.03367
G1 X158.581 Y118.531 E.99033
G1 X157.939 Y118.531 E.03367
G1 X145.21 Y131.259 E.94272
G1 X145.21 Y130.616 E.03367
G1 X157.296 Y118.531 E.89511
G1 X156.653 Y118.531 E.03367
G1 X145.21 Y129.974 E.84749
G1 X145.21 Y129.331 E.03367
G1 X156.01 Y118.531 E.79988
G1 X155.367 Y118.531 E.03367
G1 X145.21 Y128.688 E.75227
G1 X145.21 Y128.045 E.03367
G1 X154.724 Y118.531 E.70466
G1 X154.081 Y118.531 E.03367
G1 X145.21 Y127.402 E.65705
G1 X145.21 Y126.759 E.03367
G1 X153.439 Y118.531 E.60943
G1 X152.796 Y118.531 E.03367
G1 X145.21 Y126.116 E.56182
G1 X145.21 Y125.474 E.03367
G1 X152.153 Y118.531 E.51421
G1 X151.51 Y118.531 E.03367
G1 X145.21 Y124.831 E.4666
G1 X145.21 Y124.188 E.03367
G1 X150.867 Y118.531 E.41898
G1 X150.224 Y118.531 E.03367
G1 X145.21 Y123.545 E.37137
G1 X145.21 Y122.902 E.03367
G1 X149.582 Y118.531 E.32376
G1 X148.939 Y118.531 E.03367
G1 X145.21 Y122.259 E.27615
G1 X145.21 Y121.617 E.03367
G1 X148.296 Y118.531 E.22854
G1 X147.653 Y118.531 E.03367
G1 X145.21 Y120.974 E.18092
G1 X145.21 Y120.331 E.03367
G1 X147.01 Y118.531 E.13331
G1 X146.367 Y118.531 E.03367
G1 X145.21 Y119.688 E.0857
G1 X145.21 Y119.045 E.03367
G1 X145.927 Y118.329 E.05309
M106 S153
; WIPE_START
G1 X145.21 Y119.045 E-.38517
G1 X145.21 Y119.688 E-.24428
G1 X145.453 Y119.445 E-.13055
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z25.2 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z25.2
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z25.2 F4000
            G39.3 S1
            G0 Z25.2 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #124
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S0A
M204 S9000

G17
M73 P95 R11
G2 Z25.2 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z27.8 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I1 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T0
M620.1 E F523 T240
M620.10 A1 F523 L82.3188 H0.4 T240

G1 Y128 F9000




M620.11 S1 I1 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA














M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z27.8
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S0A
G392 S0

M1007 S1
M106 S153
M106 P2 S178
G1 Z28.2 F42000
G1 X15 Y241.5
G1 Z28.2
G1 Z24.8
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X15.000 Y221.000  E0.7791
G1  X50.000  E1.3302
G1  Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000 
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F3000
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G1 X52.973 Y234.413 Z25.2 F42000
G1 X138.228 Y142.912 Z25.2
G1 Z24.8
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X137.141 Y142.745 Z25.2 F42000
G1 Z24.8
G1 E.8 F1800
; FEATURE: Bridge
; LINE_WIDTH: 0.404554
; LAYER_HEIGHT: 0.4
M106 S255
G1 F3000
G1 X137.858 Y142.028 E.05309
G1 X137.858 Y141.385 E.03367
G1 X136.701 Y142.542 E.08571
G1 X136.058 Y142.542 E.03367
G1 X137.858 Y140.742 E.13332
G1 X137.858 Y140.099 E.03367
G1 X135.415 Y142.542 E.18093
G1 X134.773 Y142.542 E.03367
G1 X137.858 Y139.457 E.22854
G1 X137.858 Y138.814 E.03367
G1 X134.13 Y142.542 E.27616
G1 X133.487 Y142.542 E.03367
G1 X137.858 Y138.171 E.32377
G1 X137.858 Y137.528 E.03367
G1 X132.844 Y142.542 E.37138
G1 X132.201 Y142.542 E.03367
G1 X137.858 Y136.885 E.41899
G1 X137.858 Y136.242 E.03367
G1 X131.558 Y142.542 E.4666
G1 X130.916 Y142.542 E.03367
G1 X137.858 Y135.6 E.51422
G1 X137.858 Y134.957 E.03367
G1 X130.273 Y142.542 E.56183
G1 X129.63 Y142.542 E.03367
G1 X137.858 Y134.314 E.60944
G1 X137.858 Y133.671 E.03367
G1 X128.987 Y142.542 E.65705
G1 X128.344 Y142.542 E.03367
G1 X137.858 Y133.028 E.70467
G1 X137.858 Y132.385 E.03367
G1 X127.701 Y142.542 E.75228
G1 X127.058 Y142.542 E.03367
G1 X137.858 Y131.743 E.79989
G1 X137.858 Y131.1 E.03367
G1 X126.416 Y142.542 E.8475
G1 X125.773 Y142.542 E.03367
G1 X137.858 Y130.457 E.89511
G1 X137.858 Y129.814 E.03367
G1 X125.13 Y142.542 E.94273
G1 X124.487 Y142.542 E.03367
G1 X137.858 Y129.171 E.99034
G1 X137.858 Y128.528 E.03367
G1 X123.844 Y142.542 E1.03795
G1 X123.201 Y142.542 E.03367
G1 X137.858 Y127.886 E1.08556
G1 X137.858 Y127.243 E.03367
G1 X122.559 Y142.542 E1.13318
G1 X121.916 Y142.542 E.03367
G1 X137.858 Y126.6 E1.18079
G1 X137.858 Y125.957 E.03367
G1 X121.273 Y142.542 E1.2284
G1 X120.63 Y142.542 E.03367
G1 X137.858 Y125.314 E1.27601
G1 X137.858 Y124.671 E.03367
G1 X119.987 Y142.542 E1.32362
G1 X119.344 Y142.542 E.03367
G1 X137.858 Y124.029 E1.37124
G1 X137.858 Y123.386 E.03367
G1 X118.702 Y142.542 E1.41885
G1 X118.059 Y142.542 E.03367
G1 X137.858 Y122.743 E1.46646
G1 X137.858 Y122.1 E.03367
G1 X117.416 Y142.542 E1.51407
G1 X116.773 Y142.542 E.03367
G1 X137.858 Y121.457 E1.56169
G1 X137.858 Y120.814 E.03367
G1 X116.13 Y142.542 E1.6093
G1 X115.487 Y142.542 E.03367
G1 X137.858 Y120.172 E1.65691
G1 X137.858 Y119.529 E.03367
G1 X114.845 Y142.542 E1.70452
G1 X114.202 Y142.542 E.03366
G1 X137.858 Y118.886 E1.75213
G1 X137.215 Y118.886 E.03367
G1 X114.202 Y141.899 E1.70451
G1 X114.202 Y141.257 E.03367
G1 X136.572 Y118.886 E1.6569
G1 X135.93 Y118.886 E.03367
G1 X114.202 Y140.614 E1.60929
G1 X114.202 Y139.971 E.03367
G1 X135.287 Y118.886 E1.56168
G1 X134.644 Y118.886 E.03367
G1 X114.202 Y139.328 E1.51407
G1 X114.202 Y138.685 E.03367
G1 X134.001 Y118.886 E1.46645
G1 X133.358 Y118.886 E.03367
G1 X114.202 Y138.042 E1.41884
G1 X114.202 Y137.4 E.03367
G1 X132.715 Y118.886 E1.37123
G1 X132.073 Y118.886 E.03367
G1 X114.202 Y136.757 E1.32362
G1 X114.202 Y136.114 E.03367
G1 X131.43 Y118.886 E1.276
G1 X130.787 Y118.886 E.03367
G1 X114.202 Y135.471 E1.22839
G1 X114.202 Y134.828 E.03367
G1 X130.144 Y118.886 E1.18078
G1 X129.501 Y118.886 E.03367
G1 X114.202 Y134.185 E1.13317
G1 X114.202 Y133.543 E.03367
G1 X128.858 Y118.886 E1.08556
G1 X128.216 Y118.886 E.03367
G1 X114.202 Y132.9 E1.03794
G1 X114.202 Y132.257 E.03367
G1 X127.573 Y118.886 E.99033
G1 X126.93 Y118.886 E.03367
G1 X114.202 Y131.614 E.94272
G1 X114.202 Y130.971 E.03367
G1 X126.287 Y118.886 E.89511
G1 X125.644 Y118.886 E.03367
G1 X114.202 Y130.328 E.84749
G1 X114.202 Y129.686 E.03367
G1 X125.001 Y118.886 E.79988
G1 X124.359 Y118.886 E.03367
G1 X114.202 Y129.043 E.75227
G1 X114.202 Y128.4 E.03367
G1 X123.716 Y118.886 E.70466
G1 X123.073 Y118.886 E.03367
G1 X114.202 Y127.757 E.65705
G1 X114.202 Y127.114 E.03367
G1 X122.43 Y118.886 E.60943
G1 X121.787 Y118.886 E.03367
G1 X114.202 Y126.471 E.56182
G1 X114.202 Y125.829 E.03367
G1 X121.144 Y118.886 E.51421
G1 X120.502 Y118.886 E.03367
G1 X114.202 Y125.186 E.4666
G1 X114.202 Y124.543 E.03367
G1 X119.859 Y118.886 E.41898
G1 X119.216 Y118.886 E.03367
G1 X114.202 Y123.9 E.37137
G1 X114.202 Y123.257 E.03367
G1 X118.573 Y118.886 E.32376
G1 X117.93 Y118.886 E.03367
G1 X114.202 Y122.614 E.27615
G1 X114.202 Y121.972 E.03367
G1 X117.287 Y118.886 E.22854
G1 X116.645 Y118.886 E.03367
G1 X114.202 Y121.329 E.18092
G1 X114.202 Y120.686 E.03367
G1 X116.002 Y118.886 E.13331
G1 X115.359 Y118.886 E.03367
G1 X114.202 Y120.043 E.0857
G1 X114.202 Y119.4 E.03367
G1 X114.919 Y118.683 E.05309
M106 S153
; CHANGE_LAYER
; Z_HEIGHT: 25
; LAYER_HEIGHT: 0.200001
; WIPE_START
G1 F3000
G1 X114.202 Y119.4 E-.38517
G1 X114.202 Y120.043 E-.24428
G1 X114.445 Y119.8 E-.13055
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
; layer num/total_layer_count: 125/128
; update layer progress
M73 L125
M991 S0 P124 ;notify layer change
M106 S165.75
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G17
G3 Z25.2 I-.848 J.873 P1  F42000
G1 X138.228 Y142.912 Z25.2
G1 Z25
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
M73 P95 R10
G1 E-.04 F1800
G1 X137.082 Y135.63 Z25.4 F42000
G1 X138.064 Y119.446 Z25.4
G1 Z25
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X137.468 Y118.849 E.02603
G1 X136.932 Y118.849 E.01654
G1 X137.895 Y119.812 E.042
G1 X137.895 Y120.347 E.01654
G1 X136.397 Y118.849 E.06539
G1 X135.861 Y118.849 E.01654
G1 X137.895 Y120.883 E.08877
G1 X137.895 Y121.419 E.01654
G1 X135.325 Y118.849 E.11216
M73 P96 R10
G1 X134.79 Y118.849 E.01654
G1 X137.895 Y121.954 E.13554
G1 X137.895 Y122.49 E.01654
G1 X134.254 Y118.849 E.15893
G1 X133.718 Y118.849 E.01654
G1 X137.895 Y123.026 E.18231
G1 X137.895 Y123.562 E.01654
M73 P96 R9
G1 X133.183 Y118.849 E.2057
G1 X132.647 Y118.849 E.01654
G1 X137.895 Y124.097 E.22908
G1 X137.895 Y124.633 E.01654
G1 X132.111 Y118.849 E.25246
G1 X131.575 Y118.849 E.01654
G1 X137.895 Y125.169 E.27585
G1 X137.895 Y125.704 E.01654
G1 X131.04 Y118.849 E.29923
G1 X130.504 Y118.849 E.01654
G1 X137.895 Y126.24 E.32262
G1 X137.895 Y126.776 E.01654
G1 X129.968 Y118.849 E.346
G1 X129.433 Y118.849 E.01654
G1 X137.895 Y127.311 E.36939
G1 X137.895 Y127.847 E.01654
G1 X128.897 Y118.849 E.39277
G1 X128.361 Y118.849 E.01654
G1 X137.895 Y128.383 E.41616
G1 X137.895 Y128.919 E.01654
G1 X127.826 Y118.849 E.43954
G1 X127.29 Y118.849 E.01654
G1 X137.895 Y129.454 E.46293
G1 X137.895 Y129.99 E.01654
G1 X126.754 Y118.849 E.48631
G1 X126.218 Y118.849 E.01654
G1 X137.895 Y130.526 E.50969
G1 X137.895 Y131.061 E.01654
G1 X125.683 Y118.849 E.53308
G1 X125.147 Y118.849 E.01654
G1 X137.895 Y131.597 E.55646
G1 X137.895 Y132.133 E.01654
G1 X124.611 Y118.849 E.57985
G1 X124.076 Y118.849 E.01654
G1 X137.895 Y132.668 E.60323
G1 X137.895 Y133.204 E.01654
G1 X123.54 Y118.849 E.62662
G1 X123.004 Y118.849 E.01654
G1 X137.895 Y133.74 E.65
G1 X137.895 Y134.276 E.01654
G1 X122.469 Y118.849 E.67339
G1 X121.933 Y118.849 E.01654
G1 X137.895 Y134.811 E.69677
G1 X137.895 Y135.347 E.01654
G1 X121.397 Y118.849 E.72016
G1 X120.862 Y118.849 E.01654
G1 X137.895 Y135.883 E.74354
G1 X137.895 Y136.418 E.01654
G1 X120.326 Y118.849 E.76692
G1 X119.79 Y118.849 E.01654
G1 X137.895 Y136.954 E.79031
G1 X137.895 Y137.49 E.01654
G1 X119.254 Y118.849 E.81369
G1 X118.719 Y118.849 E.01654
G1 X137.895 Y138.025 E.83708
G1 X137.895 Y138.561 E.01654
G1 X118.183 Y118.849 E.86046
G1 X117.647 Y118.849 E.01654
G1 X137.895 Y139.097 E.88385
G1 X137.895 Y139.632 E.01654
G1 X117.112 Y118.849 E.90723
G1 X116.576 Y118.849 E.01654
G1 X137.895 Y140.168 E.93062
G1 X137.895 Y140.704 E.01654
G1 X116.04 Y118.849 E.954
G1 X115.505 Y118.849 E.01654
G1 X137.895 Y141.24 E.97739
G1 X137.895 Y141.775 E.01654
G1 X114.969 Y118.849 E1.00077
G1 X114.433 Y118.849 E.01654
G1 X137.895 Y142.311 E1.02415
G1 X137.895 Y142.579 E.00827
G1 X137.627 Y142.579 E.00827
G1 X114.165 Y119.117 E1.02415
G1 X114.165 Y119.653 E.01654
G1 X137.091 Y142.579 E1.00077
G1 X136.555 Y142.579 E.01654
G1 X114.165 Y120.189 E.97738
G1 X114.165 Y120.724 E.01654
G1 X136.02 Y142.579 E.954
G1 X135.484 Y142.579 E.01654
G1 X114.165 Y121.26 E.93061
G1 X114.165 Y121.796 E.01654
G1 X134.948 Y142.579 E.90723
G1 X134.413 Y142.579 E.01654
G1 X114.165 Y122.332 E.88384
G1 X114.165 Y122.867 E.01654
G1 X133.877 Y142.579 E.86046
G1 X133.341 Y142.579 E.01654
G1 X114.165 Y123.403 E.83707
G1 X114.165 Y123.939 E.01654
G1 X132.805 Y142.579 E.81369
G1 X132.27 Y142.579 E.01654
G1 X114.165 Y124.474 E.7903
G1 X114.165 Y125.01 E.01654
G1 X131.734 Y142.579 E.76692
G1 X131.198 Y142.579 E.01654
G1 X114.165 Y125.546 E.74354
G1 X114.165 Y126.081 E.01654
G1 X130.663 Y142.579 E.72015
G1 X130.127 Y142.579 E.01654
G1 X114.165 Y126.617 E.69677
G1 X114.165 Y127.153 E.01654
G1 X129.591 Y142.579 E.67338
G1 X129.056 Y142.579 E.01654
G1 X114.165 Y127.689 E.65
G1 X114.165 Y128.224 E.01654
G1 X128.52 Y142.579 E.62661
G1 X127.984 Y142.579 E.01654
G1 X114.165 Y128.76 E.60323
G1 X114.165 Y129.296 E.01654
G1 X127.449 Y142.579 E.57984
G1 X126.913 Y142.579 E.01654
G1 X114.165 Y129.831 E.55646
G1 X114.165 Y130.367 E.01654
G1 X126.377 Y142.579 E.53308
G1 X125.841 Y142.579 E.01654
G1 X114.165 Y130.903 E.50969
G1 X114.165 Y131.438 E.01654
G1 X125.306 Y142.579 E.48631
G1 X124.77 Y142.579 E.01654
G1 X114.165 Y131.974 E.46292
G1 X114.165 Y132.51 E.01654
G1 X124.234 Y142.579 E.43954
G1 X123.699 Y142.579 E.01654
G1 X114.165 Y133.045 E.41615
G1 X114.165 Y133.581 E.01654
G1 X123.163 Y142.579 E.39277
G1 X122.627 Y142.579 E.01654
G1 X114.165 Y134.117 E.36938
G1 X114.165 Y134.653 E.01654
G1 X122.092 Y142.579 E.346
G1 X121.556 Y142.579 E.01654
G1 X114.165 Y135.188 E.32261
G1 X114.165 Y135.724 E.01654
G1 X121.02 Y142.579 E.29923
G1 X120.484 Y142.579 E.01654
G1 X114.165 Y136.26 E.27585
G1 X114.165 Y136.795 E.01654
G1 X119.949 Y142.579 E.25246
G1 X119.413 Y142.579 E.01654
G1 X114.165 Y137.331 E.22908
G1 X114.165 Y137.867 E.01654
G1 X118.877 Y142.579 E.20569
G1 X118.342 Y142.579 E.01654
G1 X114.165 Y138.402 E.18231
G1 X114.165 Y138.938 E.01654
G1 X117.806 Y142.579 E.15892
G1 X117.27 Y142.579 E.01654
G1 X114.165 Y139.474 E.13554
G1 X114.165 Y140.01 E.01654
G1 X116.735 Y142.579 E.11215
G1 X116.199 Y142.579 E.01654
G1 X114.165 Y140.545 E.08877
G1 X114.165 Y141.081 E.01654
G1 X115.663 Y142.579 E.06538
G1 X115.127 Y142.579 E.01654
G1 X114.165 Y141.617 E.042
G1 X114.165 Y142.152 E.01654
G1 X114.761 Y142.748 E.02602
; WIPE_START
G1 X114.165 Y142.152 E-.32036
G1 X114.165 Y141.617 E-.20356
G1 X114.605 Y142.056 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z25.4 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z25.4
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z25.4 F4000
            G39.3 S1
            G0 Z25.4 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #125
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S1A
M204 S9000

G17
G2 Z25.4 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z28 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I0 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T1
M620.1 E F523 T240
M620.10 A1 F523 L119.736 H0.4 T240

G1 Y128 F9000




M620.11 S1 I0 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA



; WIPE
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
M106 P1 S0



M106 P1 S60
; FLUSH_START
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
; FLUSH_END
G1 E-2 F1800
G1 E2 F300










M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z28
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S1A
G392 S0

M1007 S1
M106 S165.75
M106 P2 S178
G1 Z28.4 F42000
G1 X15 Y221
G1 Z28.4
G1 Z25
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X50.000 Y221.000  E1.3302
G1  Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000  E0.7791
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F15000
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G1 X53.723 Y235.221 Z25.4 F42000
G1 X169.236 Y142.557 Z25.4
G1 Z25
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X168.091 Y135.276 Z25.4 F42000
G1 X169.073 Y119.091 Z25.4
G1 Z25
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X168.477 Y118.495 E.02603
G1 X167.941 Y118.495 E.01654
G1 X168.903 Y119.457 E.042
G1 X168.903 Y119.993 E.01654
G1 X167.405 Y118.495 E.06539
G1 X166.87 Y118.495 E.01654
G1 X168.903 Y120.528 E.08877
G1 X168.903 Y121.064 E.01654
G1 X166.334 Y118.495 E.11216
G1 X165.798 Y118.495 E.01654
G1 X168.903 Y121.6 E.13554
G1 X168.903 Y122.135 E.01654
G1 X165.263 Y118.495 E.15893
G1 X164.727 Y118.495 E.01654
G1 X168.903 Y122.671 E.18231
G1 X168.903 Y123.207 E.01654
G1 X164.191 Y118.495 E.2057
G1 X163.655 Y118.495 E.01654
G1 X168.903 Y123.742 E.22908
G1 X168.903 Y124.278 E.01654
G1 X163.12 Y118.495 E.25246
G1 X162.584 Y118.495 E.01654
G1 X168.903 Y124.814 E.27585
G1 X168.903 Y125.349 E.01654
G1 X162.048 Y118.495 E.29923
G1 X161.513 Y118.495 E.01654
G1 X168.903 Y125.885 E.32262
G1 X168.903 Y126.421 E.01654
G1 X160.977 Y118.495 E.346
G1 X160.441 Y118.495 E.01654
G1 X168.903 Y126.957 E.36939
G1 X168.903 Y127.492 E.01654
G1 X159.906 Y118.495 E.39277
G1 X159.37 Y118.495 E.01654
G1 X168.903 Y128.028 E.41616
G1 X168.903 Y128.564 E.01654
G1 X158.834 Y118.495 E.43954
G1 X158.298 Y118.495 E.01654
G1 X168.903 Y129.099 E.46293
G1 X168.903 Y129.635 E.01654
G1 X157.763 Y118.495 E.48631
G1 X157.227 Y118.495 E.01654
G1 X168.903 Y130.171 E.50969
G1 X168.903 Y130.706 E.01654
G1 X156.691 Y118.495 E.53308
G1 X156.156 Y118.495 E.01654
G1 X168.903 Y131.242 E.55646
G1 X168.903 Y131.778 E.01654
G1 X155.62 Y118.495 E.57985
G1 X155.084 Y118.495 E.01654
G1 X168.903 Y132.314 E.60323
G1 X168.903 Y132.849 E.01654
G1 X154.549 Y118.495 E.62662
G1 X154.013 Y118.495 E.01654
G1 X168.903 Y133.385 E.65
G1 X168.903 Y133.921 E.01654
G1 X153.477 Y118.495 E.67339
G1 X152.941 Y118.495 E.01654
G1 X168.903 Y134.456 E.69677
G1 X168.903 Y134.992 E.01654
G1 X152.406 Y118.495 E.72016
G1 X151.87 Y118.495 E.01654
G1 X168.903 Y135.528 E.74354
G1 X168.903 Y136.063 E.01654
G1 X151.334 Y118.495 E.76692
G1 X150.799 Y118.495 E.01654
G1 X168.903 Y136.599 E.79031
G1 X168.903 Y137.135 E.01654
G1 X150.263 Y118.495 E.81369
G1 X149.727 Y118.495 E.01654
G1 X168.903 Y137.671 E.83708
G1 X168.903 Y138.206 E.01654
G1 X149.192 Y118.495 E.86046
G1 X148.656 Y118.495 E.01654
G1 X168.903 Y138.742 E.88385
G1 X168.903 Y139.278 E.01654
G1 X148.12 Y118.495 E.90723
G1 X147.585 Y118.495 E.01654
G1 X168.903 Y139.813 E.93062
G1 X168.903 Y140.349 E.01654
G1 X147.049 Y118.495 E.954
G1 X146.513 Y118.495 E.01654
G1 X168.903 Y140.885 E.97739
G1 X168.903 Y141.42 E.01654
G1 X145.977 Y118.495 E1.00077
G1 X145.442 Y118.495 E.01654
G1 X168.903 Y141.956 E1.02415
G1 X168.903 Y142.224 E.00827
G1 X168.635 Y142.224 E.00827
G1 X145.174 Y118.762 E1.02415
G1 X145.174 Y119.298 E.01654
G1 X168.1 Y142.224 E1.00077
G1 X167.564 Y142.224 E.01654
G1 X145.174 Y119.834 E.97738
G1 X145.174 Y120.37 E.01654
G1 X167.028 Y142.224 E.954
G1 X166.493 Y142.224 E.01654
G1 X145.174 Y120.905 E.93061
G1 X145.174 Y121.441 E.01654
G1 X165.957 Y142.224 E.90723
G1 X165.421 Y142.224 E.01654
G1 X145.174 Y121.977 E.88384
G1 X145.174 Y122.512 E.01654
G1 X164.885 Y142.224 E.86046
G1 X164.35 Y142.224 E.01654
G1 X145.174 Y123.048 E.83707
G1 X145.174 Y123.584 E.01654
G1 X163.814 Y142.224 E.81369
G1 X163.278 Y142.224 E.01654
G1 X145.174 Y124.119 E.7903
G1 X145.174 Y124.655 E.01654
G1 X162.743 Y142.224 E.76692
G1 X162.207 Y142.224 E.01654
G1 X145.174 Y125.191 E.74354
G1 X145.174 Y125.727 E.01654
G1 X161.671 Y142.224 E.72015
G1 X161.136 Y142.224 E.01654
G1 X145.174 Y126.262 E.69677
G1 X145.174 Y126.798 E.01654
G1 X160.6 Y142.224 E.67338
G1 X160.064 Y142.224 E.01654
G1 X145.174 Y127.334 E.65
G1 X145.174 Y127.869 E.01654
G1 X159.528 Y142.224 E.62661
G1 X158.993 Y142.224 E.01654
G1 X145.174 Y128.405 E.60323
G1 X145.174 Y128.941 E.01654
G1 X158.457 Y142.224 E.57984
G1 X157.921 Y142.224 E.01654
G1 X145.174 Y129.476 E.55646
G1 X145.174 Y130.012 E.01654
G1 X157.386 Y142.224 E.53308
G1 X156.85 Y142.224 E.01654
G1 X145.174 Y130.548 E.50969
G1 X145.174 Y131.084 E.01654
M73 P96 R8
G1 X156.314 Y142.224 E.48631
G1 X155.779 Y142.224 E.01654
G1 X145.174 Y131.619 E.46292
G1 X145.174 Y132.155 E.01654
G1 X155.243 Y142.224 E.43954
G1 X154.707 Y142.224 E.01654
G1 X145.174 Y132.691 E.41615
G1 X145.174 Y133.226 E.01654
G1 X154.172 Y142.224 E.39277
G1 X153.636 Y142.224 E.01654
G1 X145.174 Y133.762 E.36938
G1 X145.174 Y134.298 E.01654
G1 X153.1 Y142.224 E.346
G1 X152.564 Y142.224 E.01654
G1 X145.174 Y134.833 E.32261
G1 X145.174 Y135.369 E.01654
G1 X152.029 Y142.224 E.29923
G1 X151.493 Y142.224 E.01654
G1 X145.174 Y135.905 E.27585
G1 X145.174 Y136.44 E.01654
G1 X150.957 Y142.224 E.25246
G1 X150.422 Y142.224 E.01654
M73 P96 R7
G1 X145.174 Y136.976 E.22908
G1 X145.174 Y137.512 E.01654
G1 X149.886 Y142.224 E.20569
G1 X149.35 Y142.224 E.01654
G1 X145.174 Y138.048 E.18231
G1 X145.174 Y138.583 E.01654
G1 X148.815 Y142.224 E.15892
G1 X148.279 Y142.224 E.01654
G1 X145.174 Y139.119 E.13554
G1 X145.174 Y139.655 E.01654
G1 X147.743 Y142.224 E.11215
G1 X147.207 Y142.224 E.01654
G1 X145.174 Y140.19 E.08877
G1 X145.174 Y140.726 E.01654
G1 X146.672 Y142.224 E.06538
G1 X146.136 Y142.224 E.01654
G1 X145.174 Y141.262 E.042
G1 X145.174 Y141.797 E.01654
G1 X145.77 Y142.394 E.02602
; CHANGE_LAYER
; Z_HEIGHT: 25.2
; LAYER_HEIGHT: 0.200001
; WIPE_START
G1 F15000
G1 X145.174 Y141.797 E-.32036
G1 X145.174 Y141.262 E-.20356
G1 X145.613 Y141.701 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
; layer num/total_layer_count: 126/128
; update layer progress
M73 L126
M991 S0 P125 ;notify layer change
M106 S170.85
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G17
M73 P97 R7
G3 Z25.4 I-.044 J1.216 P1  F42000
G1 X169.236 Y142.557 Z25.4
G1 Z25.2
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X168.307 Y142.394 Z25.6 F42000
G1 Z25.2
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X168.903 Y141.797 E.02603
G1 X168.903 Y141.262 E.01654
G1 X167.941 Y142.224 E.042
G1 X167.405 Y142.224 E.01654
G1 X168.903 Y140.726 E.06539
G1 X168.903 Y140.19 E.01654
G1 X166.87 Y142.224 E.08877
G1 X166.334 Y142.224 E.01654
G1 X168.903 Y139.655 E.11216
G1 X168.903 Y139.119 E.01654
G1 X165.798 Y142.224 E.13554
G1 X165.263 Y142.224 E.01654
G1 X168.903 Y138.583 E.15893
G1 X168.903 Y138.047 E.01654
G1 X164.727 Y142.224 E.18231
G1 X164.191 Y142.224 E.01654
G1 X168.903 Y137.512 E.2057
G1 X168.903 Y136.976 E.01654
G1 X163.655 Y142.224 E.22908
G1 X163.12 Y142.224 E.01654
G1 X168.903 Y136.44 E.25246
G1 X168.903 Y135.905 E.01654
G1 X162.584 Y142.224 E.27585
G1 X162.048 Y142.224 E.01654
G1 X168.903 Y135.369 E.29923
G1 X168.903 Y134.833 E.01654
G1 X161.513 Y142.224 E.32262
G1 X160.977 Y142.224 E.01654
G1 X168.903 Y134.298 E.346
G1 X168.903 Y133.762 E.01654
G1 X160.441 Y142.224 E.36939
G1 X159.906 Y142.224 E.01654
G1 X168.903 Y133.226 E.39277
G1 X168.903 Y132.691 E.01654
G1 X159.37 Y142.224 E.41616
G1 X158.834 Y142.224 E.01654
G1 X168.903 Y132.155 E.43954
G1 X168.903 Y131.619 E.01654
G1 X158.298 Y142.224 E.46293
G1 X157.763 Y142.224 E.01654
G1 X168.903 Y131.083 E.48631
G1 X168.903 Y130.548 E.01654
G1 X157.227 Y142.224 E.50969
G1 X156.691 Y142.224 E.01654
G1 X168.903 Y130.012 E.53308
G1 X168.903 Y129.476 E.01654
G1 X156.156 Y142.224 E.55646
G1 X155.62 Y142.224 E.01654
G1 X168.903 Y128.941 E.57985
G1 X168.903 Y128.405 E.01654
G1 X155.084 Y142.224 E.60323
G1 X154.549 Y142.224 E.01654
G1 X168.903 Y127.869 E.62662
G1 X168.903 Y127.334 E.01654
G1 X154.013 Y142.224 E.65
G1 X153.477 Y142.224 E.01654
G1 X168.903 Y126.798 E.67339
G1 X168.903 Y126.262 E.01654
G1 X152.941 Y142.224 E.69677
G1 X152.406 Y142.224 E.01654
G1 X168.903 Y125.726 E.72016
G1 X168.903 Y125.191 E.01654
G1 X151.87 Y142.224 E.74354
G1 X151.334 Y142.224 E.01654
G1 X168.903 Y124.655 E.76692
G1 X168.903 Y124.119 E.01654
G1 X150.799 Y142.224 E.79031
G1 X150.263 Y142.224 E.01654
G1 X168.903 Y123.584 E.81369
G1 X168.903 Y123.048 E.01654
G1 X149.727 Y142.224 E.83708
G1 X149.192 Y142.224 E.01654
G1 X168.903 Y122.512 E.86046
G1 X168.903 Y121.977 E.01654
G1 X148.656 Y142.224 E.88385
G1 X148.12 Y142.224 E.01654
G1 X168.903 Y121.441 E.90723
G1 X168.903 Y120.905 E.01654
G1 X147.585 Y142.224 E.93062
G1 X147.049 Y142.224 E.01654
G1 X168.903 Y120.369 E.954
G1 X168.903 Y119.834 E.01654
G1 X146.513 Y142.224 E.97739
G1 X145.977 Y142.224 E.01654
G1 X168.903 Y119.298 E1.00077
G1 X168.903 Y118.762 E.01654
G1 X145.442 Y142.224 E1.02415
G1 X145.174 Y142.224 E.00827
G1 X145.174 Y141.956 E.00827
G1 X168.635 Y118.495 E1.02415
G1 X168.1 Y118.495 E.01654
G1 X145.174 Y141.42 E1.00077
G1 X145.174 Y140.885 E.01654
G1 X167.564 Y118.495 E.97738
G1 X167.028 Y118.495 E.01654
G1 X145.174 Y140.349 E.954
G1 X145.174 Y139.813 E.01654
G1 X166.493 Y118.495 E.93061
G1 X165.957 Y118.495 E.01654
G1 X145.174 Y139.278 E.90723
G1 X145.174 Y138.742 E.01654
G1 X165.421 Y118.495 E.88384
G1 X164.885 Y118.495 E.01654
G1 X145.174 Y138.206 E.86046
G1 X145.174 Y137.67 E.01654
G1 X164.35 Y118.495 E.83707
G1 X163.814 Y118.495 E.01654
G1 X145.174 Y137.135 E.81369
G1 X145.174 Y136.599 E.01654
G1 X163.278 Y118.495 E.7903
G1 X162.743 Y118.495 E.01654
G1 X145.174 Y136.063 E.76692
G1 X145.174 Y135.528 E.01654
G1 X162.207 Y118.495 E.74354
G1 X161.671 Y118.495 E.01654
G1 X145.174 Y134.992 E.72015
G1 X145.174 Y134.456 E.01654
G1 X161.136 Y118.495 E.69677
G1 X160.6 Y118.495 E.01654
G1 X145.174 Y133.921 E.67338
G1 X145.174 Y133.385 E.01654
G1 X160.064 Y118.495 E.65
G1 X159.528 Y118.495 E.01654
G1 X145.174 Y132.849 E.62661
G1 X145.174 Y132.313 E.01654
G1 X158.993 Y118.495 E.60323
G1 X158.457 Y118.495 E.01654
G1 X145.174 Y131.778 E.57984
G1 X145.174 Y131.242 E.01654
G1 X157.921 Y118.495 E.55646
G1 X157.386 Y118.495 E.01654
G1 X145.174 Y130.706 E.53308
G1 X145.174 Y130.171 E.01654
G1 X156.85 Y118.495 E.50969
G1 X156.314 Y118.495 E.01654
G1 X145.174 Y129.635 E.48631
G1 X145.174 Y129.099 E.01654
G1 X155.779 Y118.495 E.46292
G1 X155.243 Y118.495 E.01654
G1 X145.174 Y128.564 E.43954
G1 X145.174 Y128.028 E.01654
G1 X154.707 Y118.495 E.41615
G1 X154.172 Y118.495 E.01654
G1 X145.174 Y127.492 E.39277
G1 X145.174 Y126.956 E.01654
G1 X153.636 Y118.495 E.36938
G1 X153.1 Y118.495 E.01654
G1 X145.174 Y126.421 E.346
G1 X145.174 Y125.885 E.01654
G1 X152.564 Y118.495 E.32261
G1 X152.029 Y118.495 E.01654
G1 X145.174 Y125.349 E.29923
G1 X145.174 Y124.814 E.01654
G1 X151.493 Y118.495 E.27585
G1 X150.957 Y118.495 E.01654
G1 X145.174 Y124.278 E.25246
G1 X145.174 Y123.742 E.01654
G1 X150.422 Y118.495 E.22908
G1 X149.886 Y118.495 E.01654
G1 X145.174 Y123.207 E.20569
G1 X145.174 Y122.671 E.01654
G1 X149.35 Y118.495 E.18231
G1 X148.815 Y118.495 E.01654
G1 X145.174 Y122.135 E.15892
G1 X145.174 Y121.6 E.01654
G1 X148.279 Y118.495 E.13554
G1 X147.743 Y118.495 E.01654
G1 X145.174 Y121.064 E.11215
G1 X145.174 Y120.528 E.01654
G1 X147.207 Y118.495 E.08877
M73 P97 R6
G1 X146.672 Y118.495 E.01654
G1 X145.174 Y119.992 E.06538
G1 X145.174 Y119.457 E.01654
G1 X146.136 Y118.495 E.042
G1 X145.6 Y118.495 E.01654
G1 X145.004 Y119.091 E.02602
; WIPE_START
G1 X145.6 Y118.495 E-.32036
G1 X146.136 Y118.495 E-.20356
G1 X145.697 Y118.934 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z25.6 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z25.6
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z25.6 F4000
            G39.3 S1
            G0 Z25.6 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #126
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S0A
M204 S9000

G17
G2 Z25.6 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z28.2 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I1 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T0
M620.1 E F523 T240
M620.10 A1 F523 L82.3188 H0.4 T240

G1 Y128 F9000




M620.11 S1 I1 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA














M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z28.2
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S0A
G392 S0

M1007 S1
M106 S170.85
M106 P2 S178
G1 Z28.6 F42000
G1 X50 Y221
G1 Z28.6
G1 Z25.2
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X50.000 Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000  E0.7791
G1  X50.000  E1.3302
G1  X15.000 
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F15000
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G1 X52.973 Y234.413 Z25.6 F42000
G1 X138.228 Y142.912 Z25.6
G1 Z25.2
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X137.298 Y142.748 Z25.6 F42000
G1 Z25.2
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X137.895 Y142.152 E.02603
G1 X137.895 Y141.617 E.01654
G1 X136.932 Y142.579 E.042
G1 X136.397 Y142.579 E.01654
G1 X137.895 Y141.081 E.06539
G1 X137.895 Y140.545 E.01654
G1 X135.861 Y142.579 E.08877
G1 X135.325 Y142.579 E.01654
G1 X137.895 Y140.009 E.11216
G1 X137.895 Y139.474 E.01654
G1 X134.79 Y142.579 E.13554
G1 X134.254 Y142.579 E.01654
G1 X137.895 Y138.938 E.15893
G1 X137.895 Y138.402 E.01654
G1 X133.718 Y142.579 E.18231
G1 X133.183 Y142.579 E.01654
G1 X137.895 Y137.867 E.2057
G1 X137.895 Y137.331 E.01654
G1 X132.647 Y142.579 E.22908
G1 X132.111 Y142.579 E.01654
G1 X137.895 Y136.795 E.25246
G1 X137.895 Y136.26 E.01654
G1 X131.575 Y142.579 E.27585
G1 X131.04 Y142.579 E.01654
G1 X137.895 Y135.724 E.29923
G1 X137.895 Y135.188 E.01654
G1 X130.504 Y142.579 E.32262
G1 X129.968 Y142.579 E.01654
G1 X137.895 Y134.652 E.346
G1 X137.895 Y134.117 E.01654
G1 X129.433 Y142.579 E.36939
G1 X128.897 Y142.579 E.01654
G1 X137.895 Y133.581 E.39277
G1 X137.895 Y133.045 E.01654
G1 X128.361 Y142.579 E.41616
G1 X127.826 Y142.579 E.01654
G1 X137.895 Y132.51 E.43954
G1 X137.895 Y131.974 E.01654
G1 X127.29 Y142.579 E.46293
G1 X126.754 Y142.579 E.01654
G1 X137.895 Y131.438 E.48631
G1 X137.895 Y130.903 E.01654
G1 X126.218 Y142.579 E.50969
G1 X125.683 Y142.579 E.01654
G1 X137.895 Y130.367 E.53308
G1 X137.895 Y129.831 E.01654
G1 X125.147 Y142.579 E.55646
G1 X124.611 Y142.579 E.01654
G1 X137.895 Y129.296 E.57985
G1 X137.895 Y128.76 E.01654
G1 X124.076 Y142.579 E.60323
G1 X123.54 Y142.579 E.01654
G1 X137.895 Y128.224 E.62662
G1 X137.895 Y127.688 E.01654
G1 X123.004 Y142.579 E.65
G1 X122.469 Y142.579 E.01654
G1 X137.895 Y127.153 E.67339
G1 X137.895 Y126.617 E.01654
G1 X121.933 Y142.579 E.69677
G1 X121.397 Y142.579 E.01654
G1 X137.895 Y126.081 E.72016
G1 X137.895 Y125.546 E.01654
G1 X120.862 Y142.579 E.74354
G1 X120.326 Y142.579 E.01654
G1 X137.895 Y125.01 E.76692
G1 X137.895 Y124.474 E.01654
G1 X119.79 Y142.579 E.79031
G1 X119.254 Y142.579 E.01654
G1 X137.895 Y123.939 E.81369
G1 X137.895 Y123.403 E.01654
G1 X118.719 Y142.579 E.83708
G1 X118.183 Y142.579 E.01654
G1 X137.895 Y122.867 E.86046
G1 X137.895 Y122.331 E.01654
G1 X117.647 Y142.579 E.88385
G1 X117.112 Y142.579 E.01654
G1 X137.895 Y121.796 E.90723
G1 X137.895 Y121.26 E.01654
G1 X116.576 Y142.579 E.93062
G1 X116.04 Y142.579 E.01654
G1 X137.895 Y120.724 E.954
G1 X137.895 Y120.189 E.01654
G1 X115.505 Y142.579 E.97739
G1 X114.969 Y142.579 E.01654
G1 X137.895 Y119.653 E1.00077
G1 X137.895 Y119.117 E.01654
G1 X114.433 Y142.579 E1.02415
G1 X114.165 Y142.579 E.00827
G1 X114.165 Y142.311 E.00827
G1 X137.627 Y118.849 E1.02415
G1 X137.091 Y118.849 E.01654
G1 X114.165 Y141.775 E1.00077
G1 X114.165 Y141.239 E.01654
G1 X136.555 Y118.849 E.97738
G1 X136.02 Y118.849 E.01654
G1 X114.165 Y140.704 E.954
G1 X114.165 Y140.168 E.01654
G1 X135.484 Y118.849 E.93061
G1 X134.948 Y118.849 E.01654
G1 X114.165 Y139.632 E.90723
G1 X114.165 Y139.097 E.01654
G1 X134.413 Y118.849 E.88384
G1 X133.877 Y118.849 E.01654
G1 X114.165 Y138.561 E.86046
G1 X114.165 Y138.025 E.01654
G1 X133.341 Y118.849 E.83707
G1 X132.805 Y118.849 E.01654
G1 X114.165 Y137.49 E.81369
G1 X114.165 Y136.954 E.01654
G1 X132.27 Y118.849 E.7903
G1 X131.734 Y118.849 E.01654
G1 X114.165 Y136.418 E.76692
G1 X114.165 Y135.883 E.01654
G1 X131.198 Y118.849 E.74354
G1 X130.663 Y118.849 E.01654
G1 X114.165 Y135.347 E.72015
G1 X114.165 Y134.811 E.01654
G1 X130.127 Y118.849 E.69677
G1 X129.591 Y118.849 E.01654
G1 X114.165 Y134.275 E.67338
G1 X114.165 Y133.74 E.01654
G1 X129.056 Y118.849 E.65
G1 X128.52 Y118.849 E.01654
G1 X114.165 Y133.204 E.62661
G1 X114.165 Y132.668 E.01654
G1 X127.984 Y118.849 E.60323
G1 X127.449 Y118.849 E.01654
G1 X114.165 Y132.133 E.57984
G1 X114.165 Y131.597 E.01654
G1 X126.913 Y118.849 E.55646
G1 X126.377 Y118.849 E.01654
G1 X114.165 Y131.061 E.53308
G1 X114.165 Y130.526 E.01654
G1 X125.841 Y118.849 E.50969
G1 X125.306 Y118.849 E.01654
G1 X114.165 Y129.99 E.48631
G1 X114.165 Y129.454 E.01654
G1 X124.77 Y118.849 E.46292
G1 X124.234 Y118.849 E.01654
G1 X114.165 Y128.918 E.43954
G1 X114.165 Y128.383 E.01654
G1 X123.699 Y118.849 E.41615
G1 X123.163 Y118.849 E.01654
G1 X114.165 Y127.847 E.39277
G1 X114.165 Y127.311 E.01654
G1 X122.627 Y118.849 E.36938
G1 X122.092 Y118.849 E.01654
G1 X114.165 Y126.776 E.346
G1 X114.165 Y126.24 E.01654
G1 X121.556 Y118.849 E.32261
G1 X121.02 Y118.849 E.01654
G1 X114.165 Y125.704 E.29923
G1 X114.165 Y125.169 E.01654
G1 X120.484 Y118.849 E.27585
G1 X119.949 Y118.849 E.01654
G1 X114.165 Y124.633 E.25246
G1 X114.165 Y124.097 E.01654
G1 X119.413 Y118.849 E.22908
G1 X118.877 Y118.849 E.01654
G1 X114.165 Y123.561 E.20569
G1 X114.165 Y123.026 E.01654
G1 X118.342 Y118.849 E.18231
G1 X117.806 Y118.849 E.01654
G1 X114.165 Y122.49 E.15892
G1 X114.165 Y121.954 E.01654
G1 X117.27 Y118.849 E.13554
G1 X116.735 Y118.849 E.01654
G1 X114.165 Y121.419 E.11215
G1 X114.165 Y120.883 E.01654
G1 X116.199 Y118.849 E.08877
M73 P97 R5
G1 X115.663 Y118.849 E.01654
G1 X114.165 Y120.347 E.06538
G1 X114.165 Y119.812 E.01654
G1 X115.127 Y118.849 E.042
G1 X114.592 Y118.849 E.01654
G1 X113.996 Y119.446 E.02602
; CHANGE_LAYER
; Z_HEIGHT: 25.4
; LAYER_HEIGHT: 0.199999
; WIPE_START
G1 F15000
G1 X114.592 Y118.849 E-.32036
G1 X115.127 Y118.849 E-.20356
G1 X114.688 Y119.289 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
; layer num/total_layer_count: 127/128
; update layer progress
M73 L127
M991 S0 P126 ;notify layer change
M106 S163.2
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G17
G3 Z25.6 I-.862 J.859 P1  F42000
G1 X138.228 Y142.912 Z25.6
G1 Z25.4
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X113.832 Y142.912 E.80925
G1 X113.832 Y118.516 E.80925
G1 X138.228 Y118.516 E.80925
G1 X138.228 Y142.852 E.80726
G1 X138.62 Y143.304 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
; WIPE_START
G1 X136.62 Y143.249 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X137.082 Y135.63 Z25.8 F42000
G1 X138.064 Y119.446 Z25.8
G1 Z25.4
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X137.468 Y118.849 E.02603
G1 X136.932 Y118.849 E.01654
G1 X137.895 Y119.812 E.042
G1 X137.895 Y120.347 E.01654
G1 X136.397 Y118.849 E.06539
G1 X135.861 Y118.849 E.01654
G1 X137.895 Y120.883 E.08877
G1 X137.895 Y121.419 E.01654
G1 X135.325 Y118.849 E.11216
G1 X134.79 Y118.849 E.01654
G1 X137.895 Y121.954 E.13554
G1 X137.895 Y122.49 E.01654
G1 X134.254 Y118.849 E.15893
G1 X133.718 Y118.849 E.01654
G1 X137.895 Y123.026 E.18231
G1 X137.895 Y123.562 E.01654
G1 X133.183 Y118.849 E.2057
G1 X132.647 Y118.849 E.01654
G1 X137.895 Y124.097 E.22908
G1 X137.895 Y124.633 E.01654
G1 X132.111 Y118.849 E.25246
G1 X131.575 Y118.849 E.01654
G1 X137.895 Y125.169 E.27585
G1 X137.895 Y125.704 E.01654
G1 X131.04 Y118.849 E.29923
G1 X130.504 Y118.849 E.01654
G1 X137.895 Y126.24 E.32262
G1 X137.895 Y126.776 E.01654
G1 X129.968 Y118.849 E.346
G1 X129.433 Y118.849 E.01654
G1 X137.895 Y127.311 E.36939
G1 X137.895 Y127.847 E.01654
G1 X128.897 Y118.849 E.39277
G1 X128.361 Y118.849 E.01654
G1 X137.895 Y128.383 E.41616
G1 X137.895 Y128.919 E.01654
G1 X127.826 Y118.849 E.43954
G1 X127.29 Y118.849 E.01654
G1 X137.895 Y129.454 E.46293
G1 X137.895 Y129.99 E.01654
G1 X126.754 Y118.849 E.48631
G1 X126.218 Y118.849 E.01654
M73 P98 R5
G1 X137.895 Y130.526 E.50969
G1 X137.895 Y131.061 E.01654
G1 X125.683 Y118.849 E.53308
G1 X125.147 Y118.849 E.01654
G1 X137.895 Y131.597 E.55646
G1 X137.895 Y132.133 E.01654
G1 X124.611 Y118.849 E.57985
G1 X124.076 Y118.849 E.01654
G1 X137.895 Y132.668 E.60323
G1 X137.895 Y133.204 E.01654
G1 X123.54 Y118.849 E.62662
G1 X123.004 Y118.849 E.01654
G1 X137.895 Y133.74 E.65
G1 X137.895 Y134.276 E.01654
G1 X122.469 Y118.849 E.67339
G1 X121.933 Y118.849 E.01654
G1 X137.895 Y134.811 E.69677
G1 X137.895 Y135.347 E.01654
G1 X121.397 Y118.849 E.72016
G1 X120.862 Y118.849 E.01654
G1 X137.895 Y135.883 E.74354
G1 X137.895 Y136.418 E.01654
G1 X120.326 Y118.849 E.76692
G1 X119.79 Y118.849 E.01654
G1 X137.895 Y136.954 E.79031
G1 X137.895 Y137.49 E.01654
G1 X119.254 Y118.849 E.81369
G1 X118.719 Y118.849 E.01654
G1 X137.895 Y138.025 E.83708
G1 X137.895 Y138.561 E.01654
G1 X118.183 Y118.849 E.86046
G1 X117.647 Y118.849 E.01654
G1 X137.895 Y139.097 E.88385
G1 X137.895 Y139.632 E.01654
G1 X117.112 Y118.849 E.90723
G1 X116.576 Y118.849 E.01654
G1 X137.895 Y140.168 E.93062
G1 X137.895 Y140.704 E.01654
G1 X116.04 Y118.849 E.954
G1 X115.505 Y118.849 E.01654
G1 X137.895 Y141.24 E.97739
G1 X137.895 Y141.775 E.01654
G1 X114.969 Y118.849 E1.00077
G1 X114.433 Y118.849 E.01654
G1 X137.895 Y142.311 E1.02415
G1 X137.895 Y142.579 E.00827
G1 X137.627 Y142.579 E.00827
G1 X114.165 Y119.117 E1.02415
G1 X114.165 Y119.653 E.01654
G1 X137.091 Y142.579 E1.00077
G1 X136.555 Y142.579 E.01654
G1 X114.165 Y120.189 E.97738
G1 X114.165 Y120.724 E.01654
G1 X136.02 Y142.579 E.954
G1 X135.484 Y142.579 E.01654
G1 X114.165 Y121.26 E.93061
G1 X114.165 Y121.796 E.01654
G1 X134.948 Y142.579 E.90723
G1 X134.413 Y142.579 E.01654
G1 X114.165 Y122.332 E.88384
G1 X114.165 Y122.867 E.01654
G1 X133.877 Y142.579 E.86046
G1 X133.341 Y142.579 E.01654
G1 X114.165 Y123.403 E.83707
G1 X114.165 Y123.939 E.01654
G1 X132.805 Y142.579 E.81369
G1 X132.27 Y142.579 E.01654
M73 P98 R4
G1 X114.165 Y124.474 E.7903
G1 X114.165 Y125.01 E.01654
G1 X131.734 Y142.579 E.76692
G1 X131.198 Y142.579 E.01654
G1 X114.165 Y125.546 E.74354
G1 X114.165 Y126.081 E.01654
G1 X130.663 Y142.579 E.72015
G1 X130.127 Y142.579 E.01654
G1 X114.165 Y126.617 E.69677
G1 X114.165 Y127.153 E.01654
G1 X129.591 Y142.579 E.67338
G1 X129.056 Y142.579 E.01654
G1 X114.165 Y127.689 E.65
G1 X114.165 Y128.224 E.01654
G1 X128.52 Y142.579 E.62661
G1 X127.984 Y142.579 E.01654
G1 X114.165 Y128.76 E.60323
G1 X114.165 Y129.296 E.01654
G1 X127.449 Y142.579 E.57984
G1 X126.913 Y142.579 E.01654
G1 X114.165 Y129.831 E.55646
G1 X114.165 Y130.367 E.01654
G1 X126.377 Y142.579 E.53308
G1 X125.841 Y142.579 E.01654
G1 X114.165 Y130.903 E.50969
G1 X114.165 Y131.438 E.01654
G1 X125.306 Y142.579 E.48631
G1 X124.77 Y142.579 E.01654
G1 X114.165 Y131.974 E.46292
G1 X114.165 Y132.51 E.01654
G1 X124.234 Y142.579 E.43954
G1 X123.699 Y142.579 E.01654
G1 X114.165 Y133.045 E.41615
G1 X114.165 Y133.581 E.01654
G1 X123.163 Y142.579 E.39277
G1 X122.627 Y142.579 E.01654
G1 X114.165 Y134.117 E.36938
G1 X114.165 Y134.653 E.01654
G1 X122.092 Y142.579 E.346
G1 X121.556 Y142.579 E.01654
G1 X114.165 Y135.188 E.32261
G1 X114.165 Y135.724 E.01654
G1 X121.02 Y142.579 E.29923
G1 X120.484 Y142.579 E.01654
G1 X114.165 Y136.26 E.27585
G1 X114.165 Y136.795 E.01654
G1 X119.949 Y142.579 E.25246
G1 X119.413 Y142.579 E.01654
G1 X114.165 Y137.331 E.22908
G1 X114.165 Y137.867 E.01654
G1 X118.877 Y142.579 E.20569
G1 X118.342 Y142.579 E.01654
G1 X114.165 Y138.402 E.18231
G1 X114.165 Y138.938 E.01654
G1 X117.806 Y142.579 E.15892
G1 X117.27 Y142.579 E.01654
G1 X114.165 Y139.474 E.13554
G1 X114.165 Y140.01 E.01654
G1 X116.735 Y142.579 E.11215
G1 X116.199 Y142.579 E.01654
G1 X114.165 Y140.545 E.08877
G1 X114.165 Y141.081 E.01654
G1 X115.663 Y142.579 E.06538
G1 X115.127 Y142.579 E.01654
G1 X114.165 Y141.617 E.042
G1 X114.165 Y142.152 E.01654
G1 X114.761 Y142.748 E.02602
; WIPE_START
G1 X114.165 Y142.152 E-.32036
G1 X114.165 Y141.617 E-.20356
G1 X114.605 Y142.056 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z25.8 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z25.8
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z25.8 F4000
            G39.3 S1
            G0 Z25.8 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #127
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S1A
M204 S9000

G17
G2 Z25.8 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z28.4 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I0 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T1
M620.1 E F523 T240
M620.10 A1 F523 L119.736 H0.4 T240

G1 Y128 F9000




M620.11 S1 I0 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523
G1 E0.723365 F50
G1 E8.3187 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA



; WIPE
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
M106 P1 S0



M106 P1 S60
; FLUSH_START
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
G1 E10.7763 F523
G1 E1.19736 F50
; FLUSH_END
G1 E-2 F1800
G1 E2 F300










M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z28.4
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S1A
G392 S0

M1007 S1
M106 S163.2
M106 P2 S178
G1 Z28.8 F42000
G1 X50 Y241.5
G1 Z28.8
G1 Z25.4
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X15.000 Y241.500  E1.3302
G1  Y221.000  E0.7791
G1  X50.000  E1.3302
G1  Y241.500  E0.7791
G1  X15.000 Y221.000  
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F15000
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G1 X53.723 Y235.221 Z25.8 F42000
G1 X169.236 Y142.557 Z25.8
G1 Z25.4
G1 E.8 F1800
; FEATURE: Inner wall
; LINE_WIDTH: 0.45
; LAYER_HEIGHT: 0.2
G1 F15476.087
G1 X144.841 Y142.557 E.80925
G1 X144.841 Y118.161 E.80925
G1 X169.236 Y118.161 E.80925
G1 X169.236 Y142.497 E.80726
G1 X169.629 Y142.949 F42000
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
; WIPE_START
G1 X167.629 Y142.894 E-.76
; WIPE_END
G1 E-.04 F1800
G1 X168.091 Y135.276 Z25.8 F42000
G1 X169.073 Y119.091 Z25.8
G1 Z25.4
G1 E.8 F1800
; FEATURE: Internal solid infill
; LINE_WIDTH: 0.421715
G1 F15000
G1 X168.477 Y118.495 E.02603
G1 X167.941 Y118.495 E.01654
G1 X168.903 Y119.457 E.042
G1 X168.903 Y119.993 E.01654
G1 X167.405 Y118.495 E.06539
G1 X166.87 Y118.495 E.01654
G1 X168.903 Y120.528 E.08877
G1 X168.903 Y121.064 E.01654
G1 X166.334 Y118.495 E.11216
G1 X165.798 Y118.495 E.01654
G1 X168.903 Y121.6 E.13554
G1 X168.903 Y122.135 E.01654
G1 X165.263 Y118.495 E.15893
G1 X164.727 Y118.495 E.01654
G1 X168.903 Y122.671 E.18231
G1 X168.903 Y123.207 E.01654
G1 X164.191 Y118.495 E.2057
G1 X163.655 Y118.495 E.01654
G1 X168.903 Y123.742 E.22908
G1 X168.903 Y124.278 E.01654
G1 X163.12 Y118.495 E.25246
G1 X162.584 Y118.495 E.01654
G1 X168.903 Y124.814 E.27585
G1 X168.903 Y125.349 E.01654
G1 X162.048 Y118.495 E.29923
G1 X161.513 Y118.495 E.01654
G1 X168.903 Y125.885 E.32262
G1 X168.903 Y126.421 E.01654
G1 X160.977 Y118.495 E.346
G1 X160.441 Y118.495 E.01654
G1 X168.903 Y126.957 E.36939
G1 X168.903 Y127.492 E.01654
G1 X159.906 Y118.495 E.39277
G1 X159.37 Y118.495 E.01654
G1 X168.903 Y128.028 E.41616
G1 X168.903 Y128.564 E.01654
G1 X158.834 Y118.495 E.43954
G1 X158.298 Y118.495 E.01654
G1 X168.903 Y129.099 E.46293
G1 X168.903 Y129.635 E.01654
G1 X157.763 Y118.495 E.48631
G1 X157.227 Y118.495 E.01654
G1 X168.903 Y130.171 E.50969
G1 X168.903 Y130.706 E.01654
G1 X156.691 Y118.495 E.53308
G1 X156.156 Y118.495 E.01654
G1 X168.903 Y131.242 E.55646
G1 X168.903 Y131.778 E.01654
G1 X155.62 Y118.495 E.57985
G1 X155.084 Y118.495 E.01654
G1 X168.903 Y132.314 E.60323
G1 X168.903 Y132.849 E.01654
G1 X154.549 Y118.495 E.62662
G1 X154.013 Y118.495 E.01654
G1 X168.903 Y133.385 E.65
G1 X168.903 Y133.921 E.01654
G1 X153.477 Y118.495 E.67339
G1 X152.941 Y118.495 E.01654
G1 X168.903 Y134.456 E.69677
G1 X168.903 Y134.992 E.01654
G1 X152.406 Y118.495 E.72016
G1 X151.87 Y118.495 E.01654
G1 X168.903 Y135.528 E.74354
G1 X168.903 Y136.063 E.01654
G1 X151.334 Y118.495 E.76692
G1 X150.799 Y118.495 E.01654
G1 X168.903 Y136.599 E.79031
G1 X168.903 Y137.135 E.01654
G1 X150.263 Y118.495 E.81369
G1 X149.727 Y118.495 E.01654
G1 X168.903 Y137.671 E.83708
G1 X168.903 Y138.206 E.01654
G1 X149.192 Y118.495 E.86046
G1 X148.656 Y118.495 E.01654
G1 X168.903 Y138.742 E.88385
G1 X168.903 Y139.278 E.01654
G1 X148.12 Y118.495 E.90723
G1 X147.585 Y118.495 E.01654
G1 X168.903 Y139.813 E.93062
G1 X168.903 Y140.349 E.01654
G1 X147.049 Y118.495 E.954
G1 X146.513 Y118.495 E.01654
G1 X168.903 Y140.885 E.97739
G1 X168.903 Y141.42 E.01654
G1 X145.977 Y118.495 E1.00077
G1 X145.442 Y118.495 E.01654
G1 X168.903 Y141.956 E1.02415
G1 X168.903 Y142.224 E.00827
G1 X168.635 Y142.224 E.00827
G1 X145.174 Y118.762 E1.02415
G1 X145.174 Y119.298 E.01654
G1 X168.1 Y142.224 E1.00077
G1 X167.564 Y142.224 E.01654
G1 X145.174 Y119.834 E.97738
G1 X145.174 Y120.37 E.01654
G1 X167.028 Y142.224 E.954
G1 X166.493 Y142.224 E.01654
G1 X145.174 Y120.905 E.93061
G1 X145.174 Y121.441 E.01654
G1 X165.957 Y142.224 E.90723
G1 X165.421 Y142.224 E.01654
G1 X145.174 Y121.977 E.88384
G1 X145.174 Y122.512 E.01654
G1 X164.885 Y142.224 E.86046
G1 X164.35 Y142.224 E.01654
G1 X145.174 Y123.048 E.83707
G1 X145.174 Y123.584 E.01654
G1 X163.814 Y142.224 E.81369
G1 X163.278 Y142.224 E.01654
G1 X145.174 Y124.119 E.7903
G1 X145.174 Y124.655 E.01654
G1 X162.743 Y142.224 E.76692
G1 X162.207 Y142.224 E.01654
G1 X145.174 Y125.191 E.74354
G1 X145.174 Y125.727 E.01654
G1 X161.671 Y142.224 E.72015
G1 X161.136 Y142.224 E.01654
G1 X145.174 Y126.262 E.69677
G1 X145.174 Y126.798 E.01654
G1 X160.6 Y142.224 E.67338
G1 X160.064 Y142.224 E.01654
G1 X145.174 Y127.334 E.65
G1 X145.174 Y127.869 E.01654
G1 X159.528 Y142.224 E.62661
G1 X158.993 Y142.224 E.01654
G1 X145.174 Y128.405 E.60323
G1 X145.174 Y128.941 E.01654
G1 X158.457 Y142.224 E.57984
G1 X157.921 Y142.224 E.01654
G1 X145.174 Y129.476 E.55646
G1 X145.174 Y130.012 E.01654
G1 X157.386 Y142.224 E.53308
G1 X156.85 Y142.224 E.01654
G1 X145.174 Y130.548 E.50969
G1 X145.174 Y131.084 E.01654
G1 X156.314 Y142.224 E.48631
G1 X155.779 Y142.224 E.01654
G1 X145.174 Y131.619 E.46292
G1 X145.174 Y132.155 E.01654
G1 X155.243 Y142.224 E.43954
G1 X154.707 Y142.224 E.01654
G1 X145.174 Y132.691 E.41615
G1 X145.174 Y133.226 E.01654
G1 X154.172 Y142.224 E.39277
G1 X153.636 Y142.224 E.01654
G1 X145.174 Y133.762 E.36938
G1 X145.174 Y134.298 E.01654
G1 X153.1 Y142.224 E.346
G1 X152.564 Y142.224 E.01654
G1 X145.174 Y134.833 E.32261
G1 X145.174 Y135.369 E.01654
G1 X152.029 Y142.224 E.29923
G1 X151.493 Y142.224 E.01654
M73 P98 R3
G1 X145.174 Y135.905 E.27585
G1 X145.174 Y136.44 E.01654
G1 X150.957 Y142.224 E.25246
G1 X150.422 Y142.224 E.01654
G1 X145.174 Y136.976 E.22908
G1 X145.174 Y137.512 E.01654
G1 X149.886 Y142.224 E.20569
G1 X149.35 Y142.224 E.01654
G1 X145.174 Y138.048 E.18231
G1 X145.174 Y138.583 E.01654
G1 X148.815 Y142.224 E.15892
G1 X148.279 Y142.224 E.01654
G1 X145.174 Y139.119 E.13554
G1 X145.174 Y139.655 E.01654
G1 X147.743 Y142.224 E.11215
G1 X147.207 Y142.224 E.01654
G1 X145.174 Y140.19 E.08877
G1 X145.174 Y140.726 E.01654
G1 X146.672 Y142.224 E.06538
G1 X146.136 Y142.224 E.01654
G1 X145.174 Y141.262 E.042
G1 X145.174 Y141.797 E.01654
G1 X145.77 Y142.394 E.02602
; CHANGE_LAYER
; Z_HEIGHT: 25.6
; LAYER_HEIGHT: 0.200001
; WIPE_START
G1 F15000
G1 X145.174 Y141.797 E-.32036
G1 X145.174 Y141.262 E-.20356
G1 X145.613 Y141.701 E-.23607
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
; layer num/total_layer_count: 128/128
; update layer progress
M73 L128
M991 S0 P127 ;notify layer change
M106 S168.3
; start printing object, unique label id: 100
M624 AgAAAAAAAAA=
G17
G3 Z25.8 I-.063 J1.215 P1  F42000
G1 X169.629 Y142.949 Z25.8
G1 Z25.6
G1 E.8 F1800
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
; LAYER_HEIGHT: 0.2
G1 F12000
G1 X144.449 Y142.949 E.77371
G1 X144.449 Y117.769 E.77371
G1 X169.629 Y117.769 E.77371
G1 X169.629 Y142.889 E.77187
M204 S6000
G1 X169.421 Y142.24 F42000
M204 S2000
; FEATURE: Top surface
G1 F12000
G1 X168.92 Y142.742 E.02179
G1 X168.386 Y142.742
G1 X169.421 Y141.707 E.04497
G1 X169.421 Y141.174
G1 X167.853 Y142.742 E.06814
G1 X167.32 Y142.742
G1 X169.421 Y140.641 E.09131
G1 X169.421 Y140.107
G1 X166.787 Y142.742 E.11448
G1 X166.253 Y142.742
G1 X169.421 Y139.574 E.13766
G1 X169.421 Y139.041
G1 X165.72 Y142.742 E.16083
G1 X165.187 Y142.742
G1 X169.421 Y138.507 E.184
G1 X169.421 Y137.974
G1 X164.654 Y142.742 E.20718
G1 X164.12 Y142.742
M73 P98 R2
G1 X169.421 Y137.441 E.23035
G1 X169.421 Y136.908
G1 X163.587 Y142.742 E.25352
G1 X163.054 Y142.742
G1 X169.421 Y136.374 E.2767
G1 X169.421 Y135.841
G1 X162.52 Y142.742 E.29987
G1 X161.987 Y142.742
G1 X169.421 Y135.308 E.32304
G1 X169.421 Y134.775
G1 X161.454 Y142.742 E.34622
G1 X160.921 Y142.742
G1 X169.421 Y134.241 E.36939
G1 X169.421 Y133.708
G1 X160.387 Y142.742 E.39256
G1 X159.854 Y142.742
G1 X169.421 Y133.175 E.41574
G1 X169.421 Y132.642
G1 X159.321 Y142.742 E.43891
G1 X158.788 Y142.742
G1 X169.421 Y132.108 E.46208
G1 X169.421 Y131.575
G1 X158.254 Y142.742 E.48526
G1 X157.721 Y142.742
G1 X169.421 Y131.042 E.50843
G1 X169.421 Y130.508
G1 X157.188 Y142.742 E.5316
G1 X156.654 Y142.742
G1 X169.421 Y129.975 E.55478
G1 X169.421 Y129.442
G1 X156.121 Y142.742 E.57795
G1 X155.588 Y142.742
G1 X169.421 Y128.909 E.60112
G1 X169.421 Y128.375
G1 X155.055 Y142.742 E.62429
G1 X154.521 Y142.742
G1 X169.421 Y127.842 E.64747
G1 X169.421 Y127.309
G1 X153.988 Y142.742 E.67064
G1 X153.455 Y142.742
G1 X169.421 Y126.776 E.69381
G1 X169.421 Y126.242
G1 X152.922 Y142.742 E.71699
G1 X152.388 Y142.742
G1 X169.421 Y125.709 E.74016
G1 X169.421 Y125.176
G1 X151.855 Y142.742 E.76333
G1 X151.322 Y142.742
G1 X169.421 Y124.642 E.78651
G1 X169.421 Y124.109
G1 X150.788 Y142.742 E.80968
G1 X150.255 Y142.742
G1 X169.421 Y123.576 E.83285
G1 X169.421 Y123.043
G1 X149.722 Y142.742 E.85603
G1 X149.189 Y142.742
G1 X169.421 Y122.509 E.8792
G1 X169.421 Y121.976
G1 X148.655 Y142.742 E.90237
G1 X148.122 Y142.742
G1 X169.421 Y121.443 E.92555
G1 X169.421 Y120.91
G1 X147.589 Y142.742 E.94872
G1 X147.056 Y142.742
G1 X169.421 Y120.376 E.97189
G1 X169.421 Y119.843
G1 X146.522 Y142.742 E.99507
G1 X145.989 Y142.742
G1 X169.421 Y119.31 E1.01824
G1 X169.421 Y118.776
G1 X145.456 Y142.742 E1.04141
G1 X144.923 Y142.742
G1 X169.421 Y118.243 E1.06459
G1 X169.154 Y117.977
G1 X144.656 Y142.475 E1.06458
G1 X144.656 Y141.942
G1 X168.621 Y117.977 E1.04141
G1 X168.088 Y117.977
G1 X144.656 Y141.409 E1.01823
G1 X144.656 Y140.875
G1 X167.555 Y117.977 E.99506
G1 X167.021 Y117.977
G1 X144.656 Y140.342 E.97189
G1 X144.656 Y139.809
G1 X166.488 Y117.977 E.94871
G1 X165.955 Y117.977
G1 X144.656 Y139.276 E.92554
G1 X144.656 Y138.742
G1 X165.422 Y117.977 E.90237
G1 X164.888 Y117.977
G1 X144.656 Y138.209 E.87919
G1 X144.656 Y137.676
G1 X164.355 Y117.977 E.85602
G1 X163.822 Y117.977
G1 X144.656 Y137.142 E.83285
G1 X144.656 Y136.609
G1 X163.289 Y117.977 E.80967
G1 X162.755 Y117.977
G1 X144.656 Y136.076 E.7865
G1 X144.656 Y135.543
G1 X162.222 Y117.977 E.76333
G1 X161.689 Y117.977
G1 X144.656 Y135.009 E.74015
G1 X144.656 Y134.476
G1 X161.155 Y117.977 E.71698
G1 X160.622 Y117.977
G1 X144.656 Y133.943 E.69381
G1 X144.656 Y133.41
G1 X160.089 Y117.977 E.67064
G1 X159.556 Y117.977
G1 X144.656 Y132.876 E.64746
G1 X144.656 Y132.343
G1 X159.022 Y117.977 E.62429
G1 X158.489 Y117.977
G1 X144.656 Y131.81 E.60112
G1 X144.656 Y131.276
G1 X157.956 Y117.977 E.57794
G1 X157.423 Y117.977
G1 X144.656 Y130.743 E.55477
G1 X144.656 Y130.21
G1 X156.889 Y117.977 E.5316
G1 X156.356 Y117.977
G1 X144.656 Y129.677 E.50842
G1 X144.656 Y129.143
G1 X155.823 Y117.977 E.48525
G1 X155.289 Y117.977
G1 X144.656 Y128.61 E.46208
G1 X144.656 Y128.077
G1 X154.756 Y117.977 E.4389
G1 X154.223 Y117.977
G1 X144.656 Y127.544 E.41573
G1 X144.656 Y127.01
G1 X153.69 Y117.977 E.39256
G1 X153.156 Y117.977
G1 X144.656 Y126.477 E.36938
G1 X144.656 Y125.944
G1 X152.623 Y117.977 E.34621
G1 X152.09 Y117.977
G1 X144.656 Y125.411 E.32304
G1 X144.656 Y124.877
G1 X151.557 Y117.977 E.29986
G1 X151.023 Y117.977
G1 X144.656 Y124.344 E.27669
G1 X144.656 Y123.811
G1 X150.49 Y117.977 E.25352
G1 X149.957 Y117.977
G1 X144.656 Y123.277 E.23034
G1 X144.656 Y122.744
G1 X149.423 Y117.977 E.20717
G1 X148.89 Y117.977
G1 X144.656 Y122.211 E.184
G1 X144.656 Y121.678
G1 X148.357 Y117.977 E.16083
G1 X147.824 Y117.977
G1 X144.656 Y121.144 E.13765
G1 X144.656 Y120.611
G1 X147.29 Y117.977 E.11448
G1 X146.757 Y117.977
G1 X144.656 Y120.078 E.09131
G1 X144.656 Y119.545
G1 X146.224 Y117.977 E.06813
G1 X145.691 Y117.977
G1 X144.656 Y119.011 E.04496
G1 X144.656 Y118.478
G1 X145.157 Y117.977 E.02179
M204 S6000
; WIPE_START
G1 X144.656 Y118.478 E-.26943
G1 X144.656 Y119.011 E-.20264
G1 X145.192 Y118.475 E-.28793
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 100
M625
;===================== date: 20240606 =====================

; don't support timelapse gcode in spiral_mode and by object sequence for I3 structure printer
M622.1 S1 ; for prev firware, default turned on
M1002 judge_flag timelapse_record_flag
M622 J1
G92 E0
G17
G2 Z26 I0.86 J0.86 P1 F20000 ; spiral lift a little
G1 Z26
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-48.2 F3000 ; move to safe pos
M400 P300
M971 S11 C11 O0
G92 E0
G1 X0 F18000
M623

M622.1 S1
M1002 judge_flag g39_3rd_layer_detect_flag
M622 J1
    ; enable nozzle clog detect at 3rd layer



    M622.1 S1
    M1002 judge_flag g39_detection_flag
    M622 J1

        M622.1 S0
        M1002 judge_flag g39_mass_exceed_flag
        M622 J1

            G392 S0
            M400
            G90
            M83
            M204 S5000
            G0 Z26 F4000
            G39.3 S1
            G0 Z26 F4000
            G392 S0

        M623

    M623
M623


; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
;--------------------
; CP TOOLCHANGE START
; toolchange #128
; material : PLA -> PLA
;--------------------
M220 B
M220 S100
; WIPE_TOWER_START
; filament end gcode 
M106 P3 S0

G1 E-1.2 F1800
;===== A1 20240913 =======================
M1007 S0 ; turn off mass estimation
G392 S0
M620 S0A
M204 S9000

G17
G2 Z26 I0.86 J0.86 P1 F10000 ; spiral lift a little from second lift

G1 Z28.6 F1200

M400
M106 P1 S0
M106 P2 S0

M104 S220


G1 X267 F18000


M620.11 S1 I1 E-18 F1200

M400

M620.1 E F523 T240
M620.10 A0 F523
T0
M620.1 E F523 T240
M620.10 A1 F523 L82.3188 H0.4 T240

G1 Y128 F9000




M620.11 S1 I1 E18 F523
M628 S1
G92 E0
G1 E18 F523
M400
M629 S1


M400
G92 E0
M628 S0


; FLUSH_START
; always use highest temperature to flush
M400
M1002 set_filament_type:UNKNOWN
M109 S240
M106 P1 S60

G1 E23.7 ; do not need pulsatile flushing for start part
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523
G1 E1.17238 F50
G1 E13.4823 F523

; FLUSH_END
G1 E-2 F1800
G1 E2 F300
M400
M1002 set_filament_type:PLA














M629

M400
M106 P1 S60
M109 S220
G1 E6 F523 ;Compensate for filament spillage during waiting temperature
M400
G92 E0
G1 E-2 F1800
M400
M106 P1 S178
M400 S3
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
G1 X-38.2 F18000
G1 X-48.2 F3000
M400
G1 Z28.6
M106 P1 S0

M204 S6000



M622.1 S0
M9833 F6.28466 A0.3 ; cali dynamic extrusion compensation
M1002 judge_flag filament_need_cali_flag
M622 J1
  G92 E0
  G1 E-2 F1800
  M400

  M106 P1 S178
  M400 S4
  G1 X-38.2 F18000
  G1 X-48.2 F3000
  G1 X-38.2 F18000 ;wipe and shake
  G1 X-48.2 F3000
  G1 X-38.2 F12000 ;wipe and shake
  G1 X-48.2 F3000
  M400
  M106 P1 S0 
M623

M621 S0A
G392 S0

M1007 S1
M106 S168.3
M106 P2 S178
G1 Z29 F42000
G1 X15 Y241.5
G1 Z29
G1 Z25.6
G1 E2 F1800

; filament start gcode
M106 P3 S200



G4 S0
G1  X15.000 Y221.000  E0.7791
G1  X50.000  E1.3302
G1  Y241.500  E0.7791
G1  X15.000  E1.3302
G1  Y221.000 
G1  X15.500 Y221.500  
; CP TOOLCHANGE WIPE
G1  X49.500  E1.2922 F1584
G1  Y222.833  E0.0507
G1  X15.500  E1.2922 F1800
G1  Y224.167  E0.0507
G1  X49.500  E1.2922 F2198
G1  Y225.500  E0.0507
G1  X15.500  E1.2922 F4200
G1  Y226.833  E0.0507
G1  X49.500  E1.2922 F4250
G1  Y228.167  E0.0507
G1  X15.500  E1.2922 F4300
G1  Y229.500  E0.0507
G1  X49.500  E1.2922 F4350
G1  Y230.833  E0.0507
G1  X15.500  E1.2922 F4400
G1  Y232.167  E0.0507
G1  X49.500  E1.2922 F4450
G1  Y233.500  E0.0507
G1  X15.500  E1.2922 F4500
G1  Y234.833  E0.0507
G1  X49.500  E1.2922 F4550
G1  Y236.167  E0.0507
G1  X15.500  E1.2922 F4600
G1  Y237.500  E0.0507
M73 P99 R2
G1  X49.500  E1.2922 F4650
G1  Y238.833  E0.0507
G1  X15.500  E1.2922 F4700
G1  Y240.167  E0.0507
G1  X49.500  E1.2922 F4750
; WIPE_TOWER_END
M220 R
G1 F42000
G4 S0
G92 E0
; CP TOOLCHANGE END
;------------------


G1  Y241.000  
; LAYER_HEIGHT: 0.200000
; FEATURE: Prime tower
; LINE_WIDTH: 0.500000
; WIPE_TOWER_START
G1 F5400
; WIPE_TOWER_END

; WIPE_START
G1 F12000
G1 X47.77 Y239.997 E-.76
; WIPE_END
G1 E-.04 F1800
; start printing object, unique label id: 67
M624 AQAAAAAAAAA=
G1 X52.996 Y234.435 Z26 F42000
G1 X138.62 Y143.304 Z26
G1 Z25.6
G1 E.8 F1800
M204 S5000
; FEATURE: Outer wall
; LINE_WIDTH: 0.42
; LAYER_HEIGHT: 0.2
G1 F12000
G1 X113.44 Y143.304 E.77371
G1 X113.44 Y118.124 E.77371
G1 X138.62 Y118.124 E.77371
G1 X138.62 Y143.244 E.77187
M204 S6000
G1 X138.413 Y142.595 F42000
M204 S2000
; FEATURE: Top surface
G1 F12000
G1 X137.911 Y143.097 E.02179
G1 X137.378 Y143.097
G1 X138.413 Y142.062 E.04497
G1 X138.413 Y141.529
G1 X136.845 Y143.097 E.06814
G1 X136.311 Y143.097
G1 X138.413 Y140.995 E.09131
G1 X138.413 Y140.462
G1 X135.778 Y143.097 E.11448
G1 X135.245 Y143.097
G1 X138.413 Y139.929 E.13766
G1 X138.413 Y139.396
G1 X134.711 Y143.097 E.16083
G1 X134.178 Y143.097
G1 X138.413 Y138.862 E.184
G1 X138.413 Y138.329
G1 X133.645 Y143.097 E.20718
G1 X133.112 Y143.097
G1 X138.413 Y137.796 E.23035
G1 X138.413 Y137.263
G1 X132.578 Y143.097 E.25352
G1 X132.045 Y143.097
G1 X138.413 Y136.729 E.2767
G1 X138.413 Y136.196
G1 X131.512 Y143.097 E.29987
G1 X130.979 Y143.097
G1 X138.413 Y135.663 E.32304
G1 X138.413 Y135.129
G1 X130.445 Y143.097 E.34622
G1 X129.912 Y143.097
G1 X138.413 Y134.596 E.36939
G1 X138.413 Y134.063
G1 X129.379 Y143.097 E.39256
G1 X128.846 Y143.097
G1 X138.413 Y133.53 E.41574
G1 X138.413 Y132.996
G1 X128.312 Y143.097 E.43891
G1 X127.779 Y143.097
G1 X138.413 Y132.463 E.46208
G1 X138.413 Y131.93
G1 X127.246 Y143.097 E.48526
G1 X126.712 Y143.097
G1 X138.413 Y131.397 E.50843
G1 X138.413 Y130.863
G1 X126.179 Y143.097 E.5316
G1 X125.646 Y143.097
G1 X138.413 Y130.33 E.55478
G1 X138.413 Y129.797
G1 X125.113 Y143.097 E.57795
G1 X124.579 Y143.097
G1 X138.413 Y129.263 E.60112
G1 X138.413 Y128.73
G1 X124.046 Y143.097 E.62429
G1 X123.513 Y143.097
G1 X138.413 Y128.197 E.64747
G1 X138.413 Y127.664
G1 X122.98 Y143.097 E.67064
G1 X122.446 Y143.097
G1 X138.413 Y127.13 E.69381
G1 X138.413 Y126.597
G1 X121.913 Y143.097 E.71699
G1 X121.38 Y143.097
G1 X138.413 Y126.064 E.74016
G1 X138.413 Y125.531
G1 X120.846 Y143.097 E.76333
G1 X120.313 Y143.097
G1 X138.413 Y124.997 E.78651
G1 X138.413 Y124.464
G1 X119.78 Y143.097 E.80968
G1 X119.247 Y143.097
G1 X138.413 Y123.931 E.83285
G1 X138.413 Y123.398
G1 X118.713 Y143.097 E.85603
G1 X118.18 Y143.097
G1 X138.413 Y122.864 E.8792
G1 X138.413 Y122.331
G1 X117.647 Y143.097 E.90237
G1 X117.114 Y143.097
G1 X138.413 Y121.798 E.92555
G1 X138.413 Y121.264
G1 X116.58 Y143.097 E.94872
G1 X116.047 Y143.097
G1 X138.413 Y120.731 E.97189
G1 X138.413 Y120.198
G1 X115.514 Y143.097 E.99507
G1 X114.98 Y143.097
G1 X138.413 Y119.665 E1.01824
G1 X138.413 Y119.131
G1 X114.447 Y143.097 E1.04141
G1 X113.914 Y143.097
G1 X138.413 Y118.598 E1.06459
G1 X138.146 Y118.332
G1 X113.647 Y142.83 E1.06458
G1 X113.647 Y142.297
G1 X137.613 Y118.332 E1.04141
G1 X137.079 Y118.332
G1 X113.647 Y141.763 E1.01823
G1 X113.647 Y141.23
G1 X136.546 Y118.332 E.99506
G1 X136.013 Y118.332
G1 X113.647 Y140.697 E.97189
G1 X113.647 Y140.164
G1 X135.48 Y118.332 E.94871
G1 X134.946 Y118.332
G1 X113.647 Y139.63 E.92554
G1 X113.647 Y139.097
G1 X134.413 Y118.332 E.90237
G1 X133.88 Y118.332
G1 X113.647 Y138.564 E.87919
G1 X113.647 Y138.031
G1 X133.346 Y118.332 E.85602
G1 X132.813 Y118.332
G1 X113.647 Y137.497 E.83285
G1 X113.647 Y136.964
G1 X132.28 Y118.332 E.80967
G1 X131.747 Y118.332
G1 X113.647 Y136.431 E.7865
G1 X113.647 Y135.898
G1 X131.213 Y118.332 E.76333
G1 X130.68 Y118.332
G1 X113.647 Y135.364 E.74015
G1 X113.647 Y134.831
G1 X130.147 Y118.332 E.71698
G1 X129.614 Y118.332
G1 X113.647 Y134.298 E.69381
G1 X113.647 Y133.764
G1 X129.08 Y118.332 E.67064
G1 X128.547 Y118.332
G1 X113.647 Y133.231 E.64746
G1 X113.647 Y132.698
G1 X128.014 Y118.332 E.62429
G1 X127.48 Y118.332
G1 X113.647 Y132.165 E.60112
G1 X113.647 Y131.631
G1 X126.947 Y118.332 E.57794
G1 X126.414 Y118.332
G1 X113.647 Y131.098 E.55477
G1 X113.647 Y130.565
G1 X125.881 Y118.332 E.5316
G1 X125.347 Y118.332
G1 X113.647 Y130.032 E.50842
G1 X113.647 Y129.498
G1 X124.814 Y118.332 E.48525
G1 X124.281 Y118.332
G1 X113.647 Y128.965 E.46208
G1 X113.647 Y128.432
G1 X123.748 Y118.332 E.4389
G1 X123.214 Y118.332
G1 X113.647 Y127.898 E.41573
G1 X113.647 Y127.365
G1 X122.681 Y118.332 E.39256
G1 X122.148 Y118.332
G1 X113.647 Y126.832 E.36938
G1 X113.647 Y126.299
G1 X121.615 Y118.332 E.34621
G1 X121.081 Y118.332
G1 X113.647 Y125.765 E.32304
G1 X113.647 Y125.232
G1 X120.548 Y118.332 E.29986
G1 X120.015 Y118.332
G1 X113.647 Y124.699 E.27669
G1 X113.647 Y124.166
G1 X119.481 Y118.332 E.25352
G1 X118.948 Y118.332
G1 X113.647 Y123.632 E.23034
G1 X113.647 Y123.099
G1 X118.415 Y118.332 E.20717
G1 X117.882 Y118.332
G1 X113.647 Y122.566 E.184
G1 X113.647 Y122.032
G1 X117.348 Y118.332 E.16083
G1 X116.815 Y118.332
G1 X113.647 Y121.499 E.13765
G1 X113.647 Y120.966
G1 X116.282 Y118.332 E.11448
G1 X115.749 Y118.332
G1 X113.647 Y120.433 E.09131
G1 X113.647 Y119.899
G1 X115.215 Y118.332 E.06813
G1 X114.682 Y118.332
G1 X113.647 Y119.366 E.04496
G1 X113.647 Y118.833
G1 X114.149 Y118.332 E.02179
M204 S6000
; close powerlost recovery
M1003 S0
; WIPE_START
G1 F12000
M73 P99 R1
G1 X113.647 Y118.833 E-.26943
G1 X113.647 Y119.366 E-.20264
G1 X114.183 Y118.83 E-.28793
; WIPE_END
G1 E-.04 F1800
; stop printing object, unique label id: 67
M625
M106 S0
M106 P2 S0
M981 S0 P20000 ; close spaghetti detector
; FEATURE: Custom
; filament end gcode 
M106 P3 S0
;===== date: 20231229 =====================
G392 S0 ;turn off nozzle clog detect

M400 ; wait for buffer to clear
G92 E0 ; zero the extruder
G1 E-0.8 F1800 ; retract
G1 Z26.1 F900 ; lower z a little
G1 X0 Y130.537 F18000 ; move to safe pos
G1 X-13.0 F3000 ; move to safe pos

M1002 judge_flag timelapse_record_flag
M622 J1
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M400 P100
M971 S11 C11 O0
M991 S0 P-1 ;end timelapse at safe pos
M623


M140 S0 ; turn off bed
M106 S0 ; turn off fan
M106 P2 S0 ; turn off remote part cooling fan
M106 P3 S0 ; turn off chamber cooling fan

;G1 X27 F15000 ; wipe

; pull back filament to AMS
M620 S255
G1 X267 F15000
T255
G1 X-28.5 F18000
G1 X-48.2 F3000
G1 X-28.5 F18000
G1 X-48.2 F3000
M621 S255

M104 S0 ; turn off hotend

M400 ; wait all motion done
M17 S
M17 Z0.4 ; lower z motor current to reduce impact if there is something in the bottom

    G1 Z125.6 F600
    G1 Z123.6

M400 P100
M17 R ; restore z current

G90
M73 P99 R0
G1 X-48 Y180 F3600

M220 S100  ; Reset feedrate magnitude
M201.2 K1.0 ; Reset acc magnitude
M73.2   R1.0 ;Reset left time magnitude
M1002 set_gcode_claim_speed_level : 0

;=====printer finish  sound=========
M17
M400 S1
M1006 S1
M1006 A0 B20 L100 C37 D20 M40 E42 F20 N60
M1006 A0 B10 L100 C44 D10 M60 E44 F10 N60
M1006 A0 B10 L100 C46 D10 M80 E46 F10 N80
M1006 A44 B20 L100 C39 D20 M60 E48 F20 N60
M1006 A0 B10 L100 C44 D10 M60 E44 F10 N60
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N60
M1006 A0 B10 L100 C39 D10 M60 E39 F10 N60
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N60
M1006 A0 B10 L100 C44 D10 M60 E44 F10 N60
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N60
M1006 A0 B10 L100 C39 D10 M60 E39 F10 N60
M1006 A0 B10 L100 C0 D10 M60 E0 F10 N60
M1006 A0 B10 L100 C48 D10 M60 E44 F10 N80
M1006 A0 B10 L100 C0 D10 M60 E0 F10  N80
M1006 A44 B20 L100 C49 D20 M80 E41 F20 N80
M1006 A0 B20 L100 C0 D20 M60 E0 F20 N80
M1006 A0 B20 L100 C37 D20 M30 E37 F20 N60
M1006 W
;=====printer finish  sound=========

;M17 X0.8 Y0.8 Z0.5 ; lower motor current to 45% power
M400
M18 X Y Z

M73 P100 R0
; EXECUTABLE_BLOCK_END


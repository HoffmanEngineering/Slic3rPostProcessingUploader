# Unreleased
- PrusaSlicer 3.0 support: verified against 3.0.0-alpha11 G-code, which turns `support_material` into an enum and drops `support_material_auto`
- PrusaSlicer note templates no longer render blank lines for settings that PrusaSlicer stopped writing to G-code (`support_material_auto`, `wipe_tower_x`, `wipe_tower_y`, `wipe_tower_rotation_angle`); the Full template's "Top Contact Z Distance" now reads `support_material_contact_distance`
- Added PrusaSlicer 2.9.2 and 3.0.0-alpha11 parser test fixtures

# v1.1.0
- New Slicer Support:
  - Bambu Studio
  - FLSun Slicer
  - Anycubic Slicer Next
- Add more robust Multi Material Support

# v1.0.1
- Fixed issue with Total Estimated Times not parsing correctly
- Gracefully handle missing titles

# v1.0.0
- Initial release
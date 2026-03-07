rd /s /q Zero_Ref\data

echo D | xcopy /s /y ..\StreamingAssets\data Zero_Ref\data

rd /s /q Zero_Ref\data\schemas
del Zero_Ref\data\DebugSocialAudit.csv

PAUSE
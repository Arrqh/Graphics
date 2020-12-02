from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd, get_timeout
from ...shared.utr_utils import get_repeated_utr_calls

def _cmd_base(project, platform, utr_calls, editor):
    base = [
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
    ]
    
    for utr_args in utr_calls:
        base.append(pss(f'''
         git rev-parse HEAD | git show -s --format=%%cI > revdate.tmp
         set /p GIT_REVISIONDATE=<revdate.tmp
         echo %GIT_REVISIONDATE%
         del revdate.tmp
         cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr {" ".join(utr_args)}'''))
    
    return base


def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project_folder, platform, utr_calls, editor)

    if "performance" in project["name"].lower():
        base = extra_perf_cmd(project) + install_unity_config(project) + base

    return base


def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project["folder"], platform, utr_calls, editor)

    if "performance" in project["name"].lower():
        base = extra_perf_cmd(project) + install_unity_config(project) + base

    return base

def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):

    base = [f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr.bat']
    if 'universalgraphicstest' in project["folder"].lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr {" ".join(utr_args)}')
    
    return base


def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"], utr_flags_key="utr_flags_build")
    base = _cmd_base(project["folder"], platform, utr_calls, editor)
    
    if "performance" in project["name"].lower():
        base = extra_perf_cmd(project) + install_unity_config(project) + base

    return base

def extra_perf_cmd(project):   
    perf_list = [
        f'git clone {project["url"]} -b {project["branch"]} TestProjects/{project["folder"]}',
        f'cd TestProjects/{project["folder"]} && git checkout {project["revision"]}',
        f'NetSh Advfirewall set allprofiles state off'
        ]
    return perf_list

def install_unity_config(project):
    cmds = [
        f'choco source add -n Unity -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
        f'choco install unity-config'
    ]

    for dependency in project["dependencies"]:
        cmds.append(dependency)

    return cmds
import cast.analysers.test
from cast.analysers import log
import traceback

class Runner:
    _history = {}
    @classmethod
    def fetch_analysis_result(cls, *projectPaths):
        key = ''
        for projectPath in projectPaths:
            key += projectPath.strip().lower()
        
        if key in cls._history:
            return cls._history[key]        
        analysis = cast.analysers.test.DotNetTestAnalysis()
        for projectPath in projectPaths:
            analysis.add_selection(projectPath)
        # dotnet extension version should be the last LTS
        analysis.add_dependency(r'C:\ProgramData\CAST\CAST\Extensions\com.castsoftware.dotnet.1.4.14')
        analysis.add_dependency(r'com.castsoftware.dotnetweb')
        analysis.add_dependency(r'com.castsoftware.uc.eidotnetrules\nuget\package_files')
        analysis.add_dependency(r'com.castsoftware.wbslinker')
        analysis.set_verbose(True)
        try:
            analysis.run()
        except:
            log.warning("Please confirm that you have installed com.castsoftware.dotnet version 1.4.14 !!!")
            print("================================================================================")
            print("Please confirm that you have installed com.castsoftware.dotnet version 1.4.14 !!!")
            print("================================================================================")
            log.info(str(traceback.format_exc()))
            raise

        cls._history[key] = analysis
        return analysis
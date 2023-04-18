import cast_upgrade_1_6_13
import os

from cast.analysers import dotnet, log, Bookmark
from cast.application import open_source_file
from cast import Event
from web_config_quality_rules import AvoidInsufficientSessionExpirationInConfigFile,\
                                     EnsureCookielessAreSetToUseCookies

avoid_insufficient_session_expiration = AvoidInsufficientSessionExpirationInConfigFile()
ensure_cookieless_are_set_to_use_cookies = EnsureCookielessAreSetToUseCookies()
class EIDotNetRules(dotnet.Extension):

    def __init__(self):
        self.parser = None
        self.func = None

    @Event('com.castsoftware.dotnetweb', 'dotnetweb.create_config_parser')
    def set_parser(self, create_config_parser):
        log.info("Received XML parser from com.castsoftware.dotnetweb")

        self.parser = create_config_parser(
            "XmlAttributeStatement",
            "sessionState", "FormsTagInConfig",  # http://rulesmanager/#:1c:2ou
            "security", "AllowElement"           # http://rulesmanager/#:1c:2ov
        )

        log.info("Done setting parser")

    def introducing_file(self, file):
        """
        Checks if XML configuration file violates quality rules
        """

        if not is_project_config(file):
            return

        if self.parser is None:
            log.warning("XML parser not set. Skipping analysis for file {}".format(file.get_path()))
            return

        security_token = None      # Set to <security/> element if attribute allowRemoteAccess is set to "true"
        allow_roles_admin = False  # Set to True if <allow/> element has attribute role set to "admin"

        with open_source_file(file.get_path()) as f:

            try:
                tree = self.parser.parse(f)
                for token in tree:
                    avoid_insufficient_session_expiration.check_simple_tag_attribute_values(file, token)
                    ensure_cookieless_are_set_to_use_cookies.check_simple_tag_attribute_values(file, token)

                    typename = type(token).__name__

                    # http://rulesmanager/#:1c:2ou
                    # if typename == "sessionState":
                        # In sessionState element, cookieless value by default is "UseCookies":
                        # https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/h6bb9cz9(v=vs.100)
                        # Therefore, it is NOT a violation when the cookieless attribute is absent
                        # for child in token.children:
                        #     if type(child).__name__ == "XmlAttributeStatement" and child.children[0].get_text().lower().startswith("cookieless"):
                        #         attribute_value = child.children[1]
                        #         if attribute_value.get_text()[1:-1].strip().lower() != "usecookies":
                        #             save_file_violation(file, attribute_value, "EnsureCookielessAreSetToUseCookies")
                        #         break

                    # http://rulesmanager/#:1c:2ou
                    # elif typename == "FormsTagInConfig":
                        # In forms Element for authentication, cookieless value by default is "UseDeviceProfile":
                        # https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/h6bb9cz9(v=vs.100)
                        # Therefore, it IS a violation when the cookieless attribute is absent
                        # for child in token.children:
                        #     if type(child).__name__ == "XmlAttributeStatement" and child.children[0].get_text().lower().startswith("cookieless"):
                        #         attribute_value = child.children[1]
                        #         if attribute_value.get_text()[1:-1].strip().lower() != "usecookies":
                        #             save_file_violation(file, attribute_value, "EnsureCookielessAreSetToUseCookies")
                        #         break
                        # else:
                        #     save_file_violation(file, token, "EnsureCookielessAreSetToUseCookies")

                    # http://rulesmanager/#:1c:2ov
                    if typename == "security":
                        for child in token.children:
                            if type(child).__name__ == "XmlAttributeStatement" and child.children[0].get_text().lower().startswith("allowremoteaccess"):
                                attribute_value = child.children[1]
                                if attribute_value.get_text()[1:-1].strip().lower() in ["true", "1"]:
                                    security_token = token
                                break

                    # http://rulesmanager/#:1c:2ov
                    elif typename == "AllowElement":
                        for child in token.children:
                            if type(child).__name__ == "XmlAttributeStatement" and child.children[0].get_text().lower().startswith("roles"):
                                attribute_value = child.children[1]
                                if attribute_value.get_text()[1:-1].strip().lower() == "admin":
                                    allow_roles_admin = True
                                break

                if security_token and not allow_roles_admin:
                    save_file_violation(file, security_token, "AvoidElmahEnabledInProduction")



            except Exception as e:
                log.warning("Error during parsing of web.config file: {}".format(e))

def is_project_config(file):
    """
    Checks if File is 'Web.Config' from the root of the Project directory
    """
    try:
        file_path = file.get_path()
        project_dir = os.path.dirname(file.get_project().get_fullname())
        if os.path.isfile(file_path) and os.path.samefile(os.path.dirname(file_path), project_dir):
            return os.path.basename(file_path).lower() == 'web.config'

    except:
        log.debug("Exception encountered while checking if Project Config")
        return False

def save_file_violation(file, token, rule_name):
    bookmark = Bookmark(
        file,
        token.get_begin_line(),
        token.get_begin_column(),
        token.get_end_line(),
        token.get_end_column(),
    )
    log.info("Violation of {} at {}".format(rule_name, bookmark))
    file.save_violation("CAST_EIDotNetRules_Rules_ForSourceFiles.{}".format(rule_name), bookmark)

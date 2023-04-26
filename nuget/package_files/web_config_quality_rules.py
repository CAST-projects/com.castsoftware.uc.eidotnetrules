from cast.analysers import log, Bookmark
from enum import Enum


class ValueType(Enum):
    int = 1
    string = 2
    missing_attribute = 3


class CheckType(Enum):
    forbiddenValue = 1
    mandatoryValue = 2
    forbiddenValues = 3
    mandatoryValues = 4
    max_value = 5
    min_value = 6


class XmlTagAttributeChecker:
    """

    """

    def __init__(self, tag_name, attribute_name, value_type, check_type, values):
        """

        """
        self.tag_name = tag_name.lower()
        self.attribute_name = attribute_name.lower()
        self.value_type = value_type
        self.check_type = check_type
        self.values = values
        pass

    def check_simple_tag_attribute_values(self, token):
        """

        """
        if self.value_type == ValueType.string:
            return self.check_simple_tag_attribute_string_values(token)
        elif self.value_type == ValueType.int:
            return self.check_simple_tag_attribute_integer_values(token)
        else:  # ValueType.missing_attribute
            return self.check_simple_tag_for_missing_attribute(token)

    def check_simple_tag_attribute_string_values(self, token):
        """

        """
        typename = type(token).__name__
        if typename.lower() == self.tag_name:
            for child in token.children:
                if type(child).__name__ == "XmlAttributeStatement" \
                        and child.children[0].get_text().lower().startswith(self.attribute_name):
                    attribute_value = child.children[1].get_text()[1:-1].strip().lower()
                    if self.check_type == CheckType.mandatoryValue:
                        if attribute_value != self.values[0]:
                            return False  # False mean violation
                    elif self.check_type == CheckType.mandatoryValues:
                        if attribute_value not in self.values:
                            return False  # False mean violation
                    elif self.check_type == CheckType.forbiddenValue:
                        if attribute_value == self.values[0]:
                            return False  # False mean violation
                    else:
                        if attribute_value in self.values:
                            return False  # False mean violation
        return True  # True mean no violation

    def check_simple_tag_attribute_integer_values(self, token):
        """

        """
        typename = type(token).__name__
        if typename.lower() == self.tag_name:
            for child in token.children:
                if type(child).__name__ == "XmlAttributeStatement" \
                        and child.children[0].get_text().lower().startswith(self.attribute_name):
                    attribute_value = child.children[1].get_text()[1:-1].strip()
                    if not attribute_value.isnumeric():
                        return False
                    integer_value = int(attribute_value)
                    if self.check_type == CheckType.mandatoryValue:
                        if integer_value != self.values[0]:
                            return False  # False mean violation
                    elif self.check_type == CheckType.mandatoryValues:
                        if integer_value not in self.values:
                            return False  # False mean violation
                    elif self.check_type == CheckType.forbiddenValue:
                        if integer_value == self.values[0]:
                            return False  # False mean violation
                    elif self.check_type == CheckType.forbiddenValues:
                        if integer_value in self.values:
                            return False  # False mean violation
                    elif self.check_type == CheckType.max_value:
                        if integer_value > self.values[0]:
                            return False  # False mean violation
                    else:  # CheckType.min_value
                        if integer_value < self.values[0]:
                            return False  # False mean violation
        return True  # True mean no violation

    def check_simple_tag_for_missing_attribute(self, token):
        """

        """
        typename = type(token).__name__
        if typename.lower() == self.tag_name:
            for child in token.children:
                if type(child).__name__ == "XmlAttributeStatement" \
                        and child.children[0].get_text().lower().startswith(self.attribute_name):
                    return True
            return False
        return True


class WebConfigQualityRule:
    """

    """

    def __init__(self, metamodel, xml_tag_attribute_checker=[]):
        """

        """
        self.rule_metamodel = metamodel
        self.xml_tag_attribute_checkers = xml_tag_attribute_checker
        pass

    def check_simple_tag_attribute_values(self, file, token):
        for checker in self.xml_tag_attribute_checkers:
            if not checker.check_simple_tag_attribute_values(token):
                self.save_file_violation(file, token)
        pass

    def save_file_violation(self, file, token):
        begin_line = token.get_begin_line()
        begin_column = token.get_begin_column()
        for child in token.children:
            if str(child.type) == "Token.Name.Tag":
                begin_line = child.get_begin_line()
                begin_column = child.get_begin_column()
        bookmark = Bookmark(
            file,
            begin_line,
            begin_column,
            token.get_end_line(),
            token.get_end_column(),
        )
        log.info("Violation of {} at {}".format(self.rule_metamodel, bookmark))
        file.save_violation("CAST_EIDotNetRules_Rules_ForSourceFiles.{}".format(self.rule_metamodel), bookmark)


class AvoidInsufficientSessionExpirationInConfigFile(WebConfigQualityRule):
    """

    """
    def __init__(self):
        max_expiration = 15
        xml_tag_attribute_checkers = [
            XmlTagAttributeChecker("sessionState", "timeout", ValueType.int,
                                   CheckType.max_value, [max_expiration]),
            XmlTagAttributeChecker("FormsTagInConfig", "timeout", ValueType.int,
                                   CheckType.max_value, [max_expiration]),
        ]
        super().__init__("AvoidInsufficientSessionExpirationInConfigFile", xml_tag_attribute_checkers)


class EnsureCookielessAreSetToUseCookies(WebConfigQualityRule):
    """

    """

    def __init__(self):
        xml_tag_attribute_checkers = [
            # In sessionState element, cookieless value by default is "UseCookies":
            # https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/h6bb9cz9(v=vs.100)
            # Therefore, it is NOT a violation when the cookieless attribute is absent
            XmlTagAttributeChecker("sessionState", "cookieless", ValueType.string,
                                   CheckType.mandatoryValue, ["usecookies"]),
            XmlTagAttributeChecker("FormsTagInConfig", "cookieless", ValueType.string,
                                   CheckType.mandatoryValue, ["usecookies"]),
            # In forms Element for authentication, cookieless value by default is "UseDeviceProfile":
            # https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/h6bb9cz9(v=vs.100)
            # Therefore, it IS a violation when the cookieless attribute is absent
            XmlTagAttributeChecker("FormsTagInConfig", "cookieless", ValueType.missing_attribute,
                                   CheckType.mandatoryValue, ["usecookies"]),
        ]
        super().__init__("EnsureCookielessAreSetToUseCookies", xml_tag_attribute_checkers)

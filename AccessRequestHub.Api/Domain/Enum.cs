namespace AccessRequestHub.Domain;

public enum EnvironmentType { NonProduction, Production }
public enum AccessLevelType { Read, Admin }
public enum RequestStatus { PendingManager, PendingSystemOwner, Approved, Rejected }
public enum UserRole { Requester, Manager, SystemOwner, Admin }
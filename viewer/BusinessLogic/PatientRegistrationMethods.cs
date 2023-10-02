using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using viewer.Models;
using viewer.Shared;

namespace viewer.BusinessLogic;

internal static class PatientRegistrationMethods
{
    // ConcurrentDictionary of patient registrations with firstname+lastname+insuranceId as key, bool as value
    private static ConcurrentDictionary<string, PatientAppointmentInfo> patientRegistrations = new ConcurrentDictionary<string, PatientAppointmentInfo>()
    {
        ["johndoeID12345"] = new PatientAppointmentInfo
        {
            Symptoms = new List<string> { "headache", "fever" },
            PreferredDoctorName = "Dr. Bob Seuss",
            PreferredAppointmentTimes = new List<DateTime>
            {
                new DateTime(2023, 10, 1, 10, 0, 0),
                new DateTime(2023, 10, 1, 11, 0, 0),
                new DateTime(2023, 10, 1, 12, 0, 0)
            },
            ScheduledDoctorName = "Dr. Bob Seuss",
            ScheduledAppointmentTime = new DateTime(2023, 10, 1, 12, 0, 0)
        },
        ["janedoeID45678"] = new PatientAppointmentInfo
        {
            Symptoms = new List<string> { "itchy eyes", "redness" },
            PreferredDoctorName = "Dr. Bob Seuss",
            PreferredAppointmentTimes = new List<DateTime>
            {
                new DateTime(2023, 09, 10, 10, 0, 0),
                new DateTime(2023, 09, 10, 14, 0, 0),
                new DateTime(2023, 09, 10, 16, 0, 0)
            },
            ScheduledDoctorName = "Dr. Ong",
            ScheduledAppointmentTime = new DateTime(2023, 09, 10, 14, 0, 0)
        }
    };

    public static FunctionResponse CheckIfPatientRegistered(string firstName, string lastName, string insuranceId)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(CheckIfPatientRegistered)}, Parameters = {firstName},{lastName},{insuranceId}");

        bool isPatientRegistered = false;

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(insuranceId))
        {
            errorMessage = "Patient registration failed. One or more of the required parameters are empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if insuranceId is in the format ID followed by 5 digits. if not, return error message
        if (!IsInsuranceIDValid(insuranceId))
        {
            errorMessage = "Patient registration failed. Insurance ID is not in the correct format. It should be ID followed by 5 digits.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        if (patientRegistrations.ContainsKey(GetRegistrationKey(firstName, lastName, insuranceId)))
        {
            // errorMessage = "Yes, patient is already registered.";
            isPatientRegistered = true;
        }
        else
        {
            // errorMessage = "No, patient is not registered yet. Ask the patient consent for registration.";
            isPatientRegistered = false;
        }

        // Console.WriteLine($"FunctionResponse: {errorMessage}");
        return new FunctionResponse(string.Empty, isPatientRegistered);
    }

    // Retrieve patient information from the ConcurrentDictionary for a patient who is already registered
    public static FunctionResponse RetrievePatientRegistrationInfo(string firstName, string lastName, string insuranceId)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(RetrievePatientRegistrationInfo)}, Parameters = {firstName},{lastName},{insuranceId}");
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(insuranceId))
        {
            errorMessage = "Could not retrieve patient registration. One or more of the required parameters are empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if insuranceId is in the format ID followed by 5 digits. if not, return error message
        if (!IsInsuranceIDValid(insuranceId))
        {
            errorMessage = "Could not retrieve patient registration. Insurance ID is not in the correct format. It should be ID followed by 5 digits.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        if (patientRegistrations.ContainsKey(GetRegistrationKey(firstName, lastName, insuranceId)))
        {
            return new FunctionResponse(string.Empty, patientRegistrations[GetRegistrationKey(firstName, lastName, insuranceId)]);
        }
        else
        {
            errorMessage = "The patient is not registered yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
    }

    private static bool IsInsuranceIDValid(string insuranceId)
    {
        // check if insuranceId is in the format ID followed by 5 digits. if not, return error message
        return insuranceId.StartsWith("ID") && insuranceId.Length == 7 && insuranceId.Substring(2).All(char.IsDigit);
    }

    public static FunctionResponse RegisterPatient(string firstName, string lastName, string insuranceId)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(RegisterPatient)}, Parameters = {firstName},{lastName},{insuranceId}");

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(insuranceId))
        {
            errorMessage = "Patient registration failed. One or more of the required parameters are empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if insuranceId is in the format ID followed by 5 digits. if not, return error message
        if (!IsInsuranceIDValid(insuranceId))
        {
            errorMessage = "Patient registration failed. Insurance ID is not in the correct format. It should be ID followed by 5 digits.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        if (patientRegistrations.TryAdd(GetRegistrationKey(firstName, lastName, insuranceId), new PatientAppointmentInfo()))
        {
            //errorMessage = "Patient is registered successfully. Proceed with next steps.";
        }
        else
        {
            //errorMessage = "Patient is already registered. Proceed with next steps.";
        }

        return new FunctionResponse(string.Empty, new PatientRegistrationParameters
        {
            FirstName = firstName,
            LastName = lastName,
            InsuranceId = insuranceId
        });
    }

    // A method GatherSymptoms that gets patient's symptoms as arguments and stores it in the PatientAppointmentInfo object
    public static FunctionResponse StoreSymptoms(string firstName, string lastName, string insuranceId, List<string> symptoms)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(StoreSymptoms)}, Parameters = {firstName},{lastName},{insuranceId},symptoms:");
        // symptoms?.ForEach(s => Console.WriteLine(s));

        // check if patient is registered by calling CheckIfPatientRegistered method
        var patientRegisteredCheckResult = CheckIfPatientRegistered(firstName, lastName, insuranceId);
        bool? isPatientRegistered = (bool?)patientRegisteredCheckResult.FunctionResult;
        if (!isPatientRegistered.HasValue)
        {
            errorMessage = patientRegisteredCheckResult.ErrorResponse;
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
        else if (!isPatientRegistered.Value)
        {
            errorMessage = "Patient is not registered in the system yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        if (symptoms == null || symptoms.Count == 0)
        {
            errorMessage = "Symptoms are empty. Ask patient to provide list of symptoms.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        var patientKey = GetRegistrationKey(firstName, lastName, insuranceId);

        patientRegistrations[patientKey].Symptoms = symptoms;
        return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
    }

    // A method to store patient's preferred doctor
    public static FunctionResponse StorePreferredDoctorDetails(string firstName, string lastName, string insuranceId, string preferredDoctorName)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(StorePreferredDoctorDetails)}, Parameters = {firstName},{lastName},{insuranceId},{preferredDoctorName}");

        // check if patient is registered by calling CheckIfPatientRegistered method
        var patientRegisteredCheckResult = CheckIfPatientRegistered(firstName, lastName, insuranceId);
        bool? isPatientRegistered = (bool?)patientRegisteredCheckResult.FunctionResult;
        if (!isPatientRegistered.HasValue)
        {
            errorMessage = patientRegisteredCheckResult.ErrorResponse;
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
        else if (!isPatientRegistered.Value)
        {
            errorMessage = "Patient is not registered in the system yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if preferred doctor is null or whitespace
        if (string.IsNullOrWhiteSpace(preferredDoctorName))
        {
            errorMessage = "Preferred doctor name is empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        var patientKey = GetRegistrationKey(firstName, lastName, insuranceId);

        patientRegistrations[patientKey].PreferredDoctorName = preferredDoctorName;

        // check if previous step done: check if the registered patient's PatientAppointmentInfo has Symptoms property filled
        if (patientRegistrations[patientKey].Symptoms == null || patientRegistrations[patientKey].Symptoms.Count == 0)
        {
            errorMessage = "Preferred doctor details are stored, but patient symptoms are not gathered yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
        }

        return new FunctionResponse(string.Empty, patientRegistrations[patientKey]);
    }

    // A method similar to above to store patient's preferred time slots as a list of strings, which is then parsed into a list of DateTime objects
    public static FunctionResponse StorePreferredTimeSlots(string firstName, string lastName, string insuranceId, List<string> preferredTimeSlots)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(StorePreferredTimeSlots)}, Parameters = {firstName},{lastName},{insuranceId},preferredTimeSlots:");
        // preferredTimeSlots?.ForEach(s => Console.WriteLine(s));

        // check if patient is registered by calling CheckIfPatientRegistered method
        var patientRegisteredCheckResult = CheckIfPatientRegistered(firstName, lastName, insuranceId);
        bool? isPatientRegistered = (bool?)patientRegisteredCheckResult.FunctionResult;
        if (!isPatientRegistered.HasValue)
        {
            errorMessage = patientRegisteredCheckResult.ErrorResponse;
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
        else if (!isPatientRegistered.Value)
        {
            errorMessage = "Patient is not registered in the system yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if previous step done: check if the registered patient's PatientAppointmentInfo has Symptoms property filled
        var patientKey = GetRegistrationKey(firstName, lastName, insuranceId);
        if (patientRegistrations[patientKey].Symptoms == null || patientRegistrations[patientKey].Symptoms.Count == 0)
        {
            errorMessage = "Patient symptoms are not gathered yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
        }

        // check if preferred time slots are empty
        if (preferredTimeSlots == null || preferredTimeSlots.Count == 0)
        {
            errorMessage = "Preferred time slots are empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
        }

        // parse preferred time slots into a list of DateTime objects
        List<DateTime> preferredTimeSlotsDateTime = new List<DateTime>();
        foreach (var timeSlot in preferredTimeSlots)
        {
            if (DateTime.TryParse(timeSlot, out DateTime timeSlotDateTime))
            {
                preferredTimeSlotsDateTime.Add(timeSlotDateTime);
            }
            else
            {
                errorMessage = $"Could not parse availability time provided. Date time should be in the format MM:DD:YYYY HH:MM:SS AM/PM";
                // Console.WriteLine($"FunctionResponse: {errorMessage}");
                return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
            }
        }

        patientRegistrations[patientKey].PreferredAppointmentTimes = preferredTimeSlotsDateTime;
        return new FunctionResponse(string.Empty, patientRegistrations[patientKey]);
    }

    // A method that checks preferred doctor's availability within a time range
    // and if not available, prompts user to ask if they're ok to schedule appointment with any available on-call doctor during one of the given time slots
    public static FunctionResponse CheckPreferredDoctorAvailabilityAndScheduleVisit(string firstName, string lastName, string insuranceId)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(CheckPreferredDoctorAvailabilityAndScheduleVisit)}, Parameters = {firstName},{lastName},{insuranceId}");

        // check if patient is registered by calling CheckIfPatientRegistered method
        var patientRegisteredCheckResult = CheckIfPatientRegistered(firstName, lastName, insuranceId);
        bool? isPatientRegistered = (bool?)patientRegisteredCheckResult.FunctionResult;
        if (!isPatientRegistered.HasValue)
        {
            errorMessage = patientRegisteredCheckResult.ErrorResponse;
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
        else if (!isPatientRegistered.Value)
        {
            errorMessage = "Patient is not registered in the system yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        var patientKey = GetRegistrationKey(firstName, lastName, insuranceId);
        // check if preferred doctor is null or whitespace
        if (string.IsNullOrWhiteSpace(patientRegistrations[patientKey].PreferredDoctorName))
        {
            errorMessage = "Required parameter - preferred doctor name is empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        if (patientRegistrations[patientKey].PreferredAppointmentTimes == null || patientRegistrations[patientKey].PreferredAppointmentTimes.Count == 0)
        {
            errorMessage = "Preferred availability times are required and is not provided yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if preferred doctor is available during preferred times
        (bool isPreferredDoctorAvailable, DateTime? availableTimeSlot) = GetDoctorAvailability(patientRegistrations[patientKey].PreferredDoctorName, patientRegistrations[patientKey].PreferredAppointmentTimes);
        if (isPreferredDoctorAvailable)
        {
            var appointmentResult = ScheduleAppointmentWithAGivenDoctor(firstName, lastName, insuranceId, patientRegistrations[patientKey].PreferredDoctorName, availableTimeSlot.ToString());

            if (appointmentResult.FunctionResult != null)
            {
                return new FunctionResponse(string.Empty, patientRegistrations[patientKey]);
            }
            else
            {
                errorMessage = appointmentResult.ErrorResponse;
                // Console.WriteLine($"FunctionResponse: {errorMessage}");
                return new FunctionResponse(errorMessage, null);
            }
        }
        else
        {
            errorMessage = "Preferred doctor is not available during preferred time slots.";
            // create a new jobject with alternate preferred time slots and assign it to the functionResult object in FunctionResponse
            JObject alternatePreferredTimeSlotsForDoctor1 = new JObject()
            {
                { "alternateDoctorName", "Dr. Sam Smith" },
                { "alternatePreferredTimeSlots", $"{DateTime.Now}, {DateTime.Now.AddHours(5)}, {DateTime.Now.AddHours(15)}" }
            };
            JObject alternatePreferredTimeSlotsForDoctor2 = new JObject()
            {
                { "alternateDoctorName", "Dr. Bob Seuss" },
                { "alternatePreferredTimeSlots", $"{DateTime.Now.AddHours(10)}, {DateTime.Now.AddHours(7)}, {DateTime.Now.AddHours(15)}" }
            };
            JArray alternatePreferredTimeSlots = new JArray()
            {
                alternatePreferredTimeSlotsForDoctor1,
                alternatePreferredTimeSlotsForDoctor2
            };

            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, alternatePreferredTimeSlots);
        }
    }

    // A method that schedules an appointment with a given doctor's name and time slot
    public static FunctionResponse ScheduleAppointmentWithAGivenDoctor(string firstName, string lastName, string insuranceId, string doctorName, string timeSlot)
    {
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(ScheduleAppointmentWithAGivenDoctor)}, Parameters = {firstName},{lastName},{insuranceId},{doctorName},{timeSlot}");
        string errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(doctorName))
        {
            errorMessage = "Required parameter - doctor name is empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        if (string.IsNullOrWhiteSpace(timeSlot))
        {
            errorMessage = "Required parameter - time slot is empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // VAlidate that timeSlot can be parsed to DateTime
        if (!DateTime.TryParse(timeSlot, out DateTime timeSlotDateTime))
        {
            errorMessage = $"Could not parse time slot provided. Date time should be in the format MM:DD:YYYY HH:MM:SS AM/PM";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        // check if patient is registered by calling CheckIfPatientRegistered method
        var patientRegisteredCheckResult = CheckIfPatientRegistered(firstName, lastName, insuranceId);
        bool? isPatientRegistered = (bool?)patientRegisteredCheckResult.FunctionResult;
        if (!isPatientRegistered.HasValue)
        {
            errorMessage = patientRegisteredCheckResult.ErrorResponse;
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
        else if (!isPatientRegistered.Value)
        {
            errorMessage = "Patient is not registered in the system yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        var patientKey = GetRegistrationKey(firstName, lastName, insuranceId);
        if (patientRegistrations[patientKey].Symptoms == null || patientRegistrations[patientKey].Symptoms.Count == 0)
        {
            errorMessage = "Patient symptoms are not gathered yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
        }

        // display message that appointment is scheduled with preferred doctor
        patientRegistrations[patientKey].ScheduledDoctorName = doctorName;
        patientRegistrations[patientKey].ScheduledAppointmentTime = timeSlotDateTime;
        return new FunctionResponse(string.Empty, patientRegistrations[patientKey]);
    }

    // A method that schedules an appointment with an on-call doctor during preferred time slots
    public static FunctionResponse ScheduleAppointmentWithOnCallDoctor(string firstName, string lastName, string insuranceId)
    {
        string errorMessage = string.Empty;
        // Console.WriteLine($"FunctionCalled: FunctionName = {nameof(ScheduleAppointmentWithOnCallDoctor)}, Parameters = {firstName},{lastName},{insuranceId}");

        // check if patient is registered by calling CheckIfPatientRegistered method
        var patientRegisteredCheckResult = CheckIfPatientRegistered(firstName, lastName, insuranceId);
        bool? isPatientRegistered = (bool?)patientRegisteredCheckResult.FunctionResult;
        if (!isPatientRegistered.HasValue)
        {
            errorMessage = patientRegisteredCheckResult.ErrorResponse;
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }
        else if (!isPatientRegistered.Value)
        {
            errorMessage = "Patient is not registered in the system yet.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, null);
        }

        var patientKey = GetRegistrationKey(firstName, lastName, insuranceId);
        if (patientRegistrations[patientKey].PreferredAppointmentTimes == null || patientRegistrations[patientKey].PreferredAppointmentTimes.Count == 0)
        {
            errorMessage = "Preferred availability times are empty.";
            // Console.WriteLine($"FunctionResponse: {errorMessage}");
            return new FunctionResponse(errorMessage, patientRegistrations[patientKey]);
        }

        // display message that appointment is scheduled with on-call doctor
        // errorMessage = $"Appointment is scheduled with on-call doctor, Dr. Ong, during patient's preferred time slot {patientRegistrations[patientKey].PreferredAppointmentTimes.FirstOrDefault()}. End conversation now with a polite message.";
        patientRegistrations[patientKey].ScheduledDoctorName = "Dr. Ong";
        patientRegistrations[patientKey].ScheduledAppointmentTime = patientRegistrations[patientKey].PreferredAppointmentTimes.FirstOrDefault();
        return new FunctionResponse(string.Empty, patientRegistrations[patientKey]);
    }

    private static (bool, DateTime?) GetDoctorAvailability(string preferredDoctorName, List<DateTime> preferredAppointmentTimes)
    {
        if (preferredDoctorName.EndsWith("bob seuss", StringComparison.OrdinalIgnoreCase) ||
           (preferredDoctorName.EndsWith("sam smith", StringComparison.OrdinalIgnoreCase)))
        {
            return (true, preferredAppointmentTimes.FirstOrDefault());
        }

        return (false, null);
    }

    // A method that returns the list of all functions available to the user from the static OpenAIFunctions class using reflection. Return type is a list of MethodInfo objects
    public static Dictionary<string, (MethodInfo, Type)> GetAvailableFunctions()
    {
        var availableFunctions = new Dictionary<string, (MethodInfo, Type)>();
        var patientRegistrationMethodsType = typeof(PatientRegistrationMethods);

        // ToDo: Add method parameters class into the dictionary
        // availableFunctions.Add(nameof(CheckIfPatientRegistered), (patientRegistrationMethodsType.GetMethod(nameof(CheckIfPatientRegistered)), typeof(PatientRegistrationParameters)));
        availableFunctions.Add(nameof(RetrievePatientRegistrationInfo), (patientRegistrationMethodsType.GetMethod(nameof(RetrievePatientRegistrationInfo)), typeof(PatientRegistrationParameters)));
        availableFunctions.Add(nameof(RegisterPatient), (patientRegistrationMethodsType.GetMethod(nameof(RegisterPatient)), typeof(PatientRegistrationParameters)));
        availableFunctions.Add(nameof(StoreSymptoms), (patientRegistrationMethodsType.GetMethod(nameof(StoreSymptoms)), typeof(StoreSymptomsParameters)));
        availableFunctions.Add(nameof(StorePreferredDoctorDetails), (patientRegistrationMethodsType.GetMethod(nameof(StorePreferredDoctorDetails)), typeof(StorePreferredDoctorDetailsParameters)));
        availableFunctions.Add(nameof(StorePreferredTimeSlots), (patientRegistrationMethodsType.GetMethod(nameof(StorePreferredTimeSlots)), typeof(StorePreferredTimeSlotsParameters)));
        availableFunctions.Add(nameof(CheckPreferredDoctorAvailabilityAndScheduleVisit), (patientRegistrationMethodsType.GetMethod(nameof(CheckPreferredDoctorAvailabilityAndScheduleVisit)), typeof(PatientRegistrationParameters)));
        availableFunctions.Add(nameof(ScheduleAppointmentWithAGivenDoctor), (patientRegistrationMethodsType.GetMethod(nameof(ScheduleAppointmentWithAGivenDoctor)), typeof(ScheduleGivenDoctorAndTimeParameters)));
        availableFunctions.Add(nameof(ScheduleAppointmentWithOnCallDoctor), (patientRegistrationMethodsType.GetMethod(nameof(ScheduleAppointmentWithOnCallDoctor)), typeof(PatientRegistrationParameters)));

        return availableFunctions;
    }

    public static IList<AIFunctionDto> GetFunctionDefinitions()
    {
        //var checkPatientFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(CheckIfPatientRegistered),
        //    "Checks if the patient is already registered. The function returns true or false or an error message if there was an error.",
        //    typeof(PatientRegistrationParameters));

        var retrievePatientFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
            nameof(RetrievePatientRegistrationInfo),
            "Retrieves the patient's registration details. If found, the function returns the registered patient details, else an error message.",
            typeof(PatientRegistrationParameters));

        var registerPatientFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
            nameof(RegisterPatient),
            "Registers a new patient. If successful, the function returns the registered patient details, else an error message.",
            typeof(PatientRegistrationParameters));

        //var storeSymptomsFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(StoreSymptoms),
        //    "Stores symptoms from the patient. If successful, the function returns the registered patient details, else an error message.",
        //    typeof(StoreSymptomsParameters));

        //var storePreferredDoctorDetailsFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(StorePreferredDoctorDetails),
        //    "Stores the patient's preferred doctor's name. If successful, the function returns the registered patient details, else an error message.",
        //    typeof(StorePreferredDoctorDetailsParameters));

        //var storePreferredTimeSlotsFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(StorePreferredTimeSlots),
        //    "Stores preferred appointment time slots as a list of strings in C# DateTime format from the patient. If successful, the function returns the registered patient details, else an error message.",
        //    typeof(StorePreferredTimeSlotsParameters));

        //var checkPreferredDoctorAvailabilityAndScheduleVisitFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(CheckPreferredDoctorAvailabilityAndScheduleVisit),
        //    "Checks if the preferred doctor is available during any of the patient's preferred time slots. If available, it schedules the appointment and returns the appointment details. If not available, it returns a list of alternate doctors and time slots.",
        //    typeof(PatientRegistrationParameters));

        //var scheduleAppointmentWithAGivenDoctorFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(ScheduleAppointmentWithAGivenDoctor),
        //    "Schedules an appointment with the given doctor in the given time slot and returns the appointment details.",
        //    typeof(ScheduleGivenDoctorAndTimeParameters));

        //var scheduleAppointmentWithOnCallDoctorFunctionDefinition = JsonExtractionUtils.GetFunctionDefinition(
        //    nameof(ScheduleAppointmentWithOnCallDoctor),
        //    "Schedules an appointment with the on-call doctor during one of the patient's preferred time slot. If successful, the function returns the appointment details, else an error message.",
        //    typeof(PatientRegistrationParameters));

        return new List<AIFunctionDto>
        {
            retrievePatientFunctionDefinition,
            registerPatientFunctionDefinition,
            //storeSymptomsFunctionDefinition,
            //storePreferredDoctorDetailsFunctionDefinition,
            //storePreferredTimeSlotsFunctionDefinition,
            //checkPreferredDoctorAvailabilityAndScheduleVisitFunctionDefinition,
            //scheduleAppointmentWithAGivenDoctorFunctionDefinition,
            //scheduleAppointmentWithOnCallDoctorFunctionDefinition
        };
    }

    private static string GetRegistrationKey(string firstName, string lastName, string insuranceId)
    {
        return firstName.ToLower() + lastName.ToLower() + insuranceId.ToLower();
    }

    // a class FunctionResponse with a string and object property
    public class FunctionResponse
    {
        // Message that is returned to the AI with the ChatRole = system to nudge it to the next step.
        [JsonProperty("errorResponse")]
        [PropertyDescription("Error message describing the failure from the function call.")]
        public string ErrorResponse { get; set; }

        // Result of the function execution.
        [JsonProperty("functionResult")]
        [PropertyDescription("The object representing the result of a successful function call.")]
        public object? FunctionResult { get; set; }

        public FunctionResponse(string errorResponse, object? functionResult)
        {
            ErrorResponse = errorResponse;
            FunctionResult = functionResult;
        }
    }
}

internal class ScheduleGivenDoctorAndTimeParameters
{
    [JsonProperty("firstName")]
    [PropertyDescription("The patient's first name")] // ToDo: can we use System.ComponentModel.DescriptionAttribute instead?
    [JsonRequired]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    [PropertyDescription("The patient's last name")]
    [JsonRequired]
    public string LastName { get; set; }

    [JsonProperty("insuranceId")]
    [PropertyDescription("The patient's insurance ID. Insurance ID should strictly adhere to this format: ID followed by a 5 digit number, for example ID23345")]
    [JsonRequired]
    public string InsuranceId { get; set; }

    [JsonProperty("doctorName")]
    [PropertyDescription("Name of the doctor with whom the patient wants to schedule an appointment")]
    [JsonRequired]
    public string DoctorName { get; set; }

    [JsonProperty("timeSlot")]
    [PropertyDescription("Time slot in which the patient wants to schedule an appointment with the doctor.")]
    [JsonRequired]
    public string TimeSlot { get; set; }
}

public class PatientRegistrationParameters
{
    [JsonProperty("firstName")]
    [PropertyDescription("The patient's first name")] // ToDo: can we use System.ComponentModel.DescriptionAttribute instead?
    [JsonRequired]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    [PropertyDescription("The patient's last name")]
    [JsonRequired]
    public string LastName { get; set; }

    [JsonProperty("insuranceId")]
    [PropertyDescription("The patient's insurance ID. Insurance ID should strictly adhere to this format: ID followed by a 5 digit number, for example ID23345.")]
    [JsonRequired]
    public string InsuranceId { get; set; }
}

public class StoreSymptomsParameters
{
    [JsonProperty("firstName")]
    [PropertyDescription("The patient's first name")] // ToDo: can we use System.ComponentModel.DescriptionAttribute instead?
    [JsonRequired]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    [PropertyDescription("The patient's last name")]
    [JsonRequired]
    public string LastName { get; set; }

    [JsonProperty("insuranceId")]
    [PropertyDescription("The patient's insurance ID. Insurance ID should strictly adhere to this format: ID followed by a 5 digit number, for example ID23345.")]
    [JsonRequired]
    public string InsuranceId { get; set; }

    [JsonProperty("symptoms")]
    [PropertyDescription("The list of patient's symptoms")]
    [JsonRequired]
    public List<string> Symptoms { get; set; }
}

public class StorePreferredDoctorDetailsParameters
{
    [JsonProperty("firstName")]
    [PropertyDescription("The patient's first name")] // ToDo: can we use System.ComponentModel.DescriptionAttribute instead?
    [JsonRequired]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    [PropertyDescription("The patient's last name")]
    [JsonRequired]
    public string LastName { get; set; }

    [JsonProperty("insuranceId")]
    [PropertyDescription("The patient's insurance ID. Insurance ID should strictly adhere to this format: ID followed by a 5 digit number, for example ID23345.")]
    [JsonRequired]
    public string InsuranceId { get; set; }

    [JsonProperty("preferredDoctorName")]
    [PropertyDescription("The patient's preferred doctor's name")]
    [JsonRequired]
    public string PreferredDoctorName { get; set; }
}

public class StorePreferredTimeSlotsParameters
{
    [JsonProperty("firstName")]
    [PropertyDescription("The patient's first name")] // ToDo: can we use System.ComponentModel.DescriptionAttribute instead?
    [JsonRequired]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    [PropertyDescription("The patient's last name")]
    [JsonRequired]
    public string LastName { get; set; }

    [JsonProperty("insuranceId")]
    [PropertyDescription("The patient's insurance ID. Insurance ID should strictly adhere to this format: ID followed by a 5 digit number, for example ID23345.")]
    [JsonRequired]
    public string InsuranceId { get; set; }

    [JsonProperty("preferredAppointmentTimes")]
    [PropertyDescription("The list of patient's preferred appointment times")]
    [JsonRequired]
    public List<string> PreferredAppointmentTimes { get; set; }
}

public class PatientAppointmentInfo
{
    public List<string> Symptoms { get; set; }

    public string PreferredDoctorName { get; set; }

    public List<DateTime> PreferredAppointmentTimes { get; set; }

    public string ScheduledDoctorName { get; set; }

    public DateTime ScheduledAppointmentTime { get; set; }
}
